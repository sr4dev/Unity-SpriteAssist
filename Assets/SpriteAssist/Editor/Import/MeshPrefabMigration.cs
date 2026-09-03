using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    // 旧構造（Mesh が prefab に埋め込み）の Mesh Prefab を、テクスチャのサブアセット Mesh を参照する新構造へ移行する。
    // Project ウィンドウの選択範囲、または確認付きのプロジェクト全体メニューから実行する。
    public static class MeshPrefabMigration
    {
        public struct Result
        {
            public int migrated;
            // v1.5.1 との API 互換性のため残す。orphan の自動リンクは行わないため常に 0。
            public int linked;
            public int skipped;
        }

        private const string MigrateSelectedMenu = "Assets/SpriteAssist/Migrate Legacy Mesh Prefabs (Selected)";
        private const string MigrateAllMenu = "Assets/SpriteAssist/Migrate Legacy Mesh Prefabs (Entire Project)";

        // 大量処理時のメモリ対策。AssetDatabase.LoadAssetAtPath 等で読み込んだテクスチャ/prefab は明示的に解放しないと
        // 処理が終わるまでメモリに残り続けるため、一定件数ごとに保存・解放する。
        private const int ScanUnloadInterval = 200;
        private const int ReimportBatchSize = 100;
        private const int MigrateFlushInterval = 50;

        // 選択中のテクスチャ / prefab / フォルダ（サブフォルダ含む）を対象にする
        [MenuItem(MigrateSelectedMenu, priority = 710)]
        private static void MigrateSelectedMenuItem()
        {
            string[] selectedPaths = GetSelectedAssetPaths();
            ShowResult(Migrate(selectedPaths), "selection");
        }

        [MenuItem(MigrateSelectedMenu, validate = true)]
        private static bool MigrateSelectedMenuItemValidate()
        {
            return GetSelectedAssetPaths().Length > 0;
        }

        [MenuItem(MigrateAllMenu, priority = 711)]
        private static void MigrateAllMenuItem()
        {
            if (!EditorUtility.DisplayDialog("SpriteAssist",
                    "Scan the entire project and migrate every legacy Mesh Prefab?\n\nTextures linked to legacy prefabs will be reimported.", "Migrate", "Cancel"))
            {
                return;
            }

            ShowResult(MigrateAll(), "entire project");
        }

        private static void ShowResult(Result result, string scope)
        {
            string skipped = result.skipped > 0 ? $"\nSkipped: {result.skipped} (see Console)" : string.Empty;
            EditorUtility.DisplayDialog("SpriteAssist",
                $"Mesh Prefab migration ({scope})\n\nMigrated: {result.migrated}{skipped}", "OK");
        }

        public static Result MigrateAll()
        {
            return Migrate(new[] { "Assets" });
        }

        // assetPaths: テクスチャ / prefab / フォルダ。フォルダは再帰的に走査する。
        public static Result Migrate(IEnumerable<string> assetPaths)
        {
            var result = new Result();
            var candidates = new List<Candidate>();
            var texturePathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                int scanned = 0;
                foreach (string assetPath in ExpandAssetPaths(assetPaths))
                {
                    if (TryCreateCandidate(assetPath, out Candidate candidate, out string skipReason))
                    {
                        if (texturePathSet.Add(candidate.texturePath))
                        {
                            candidates.Add(candidate);
                        }
                    }
                    else if (skipReason != null)
                    {
                        result.skipped++;
                        Debug.LogWarning($"[SpriteAssist] Skipped '{assetPath}': {skipReason}");
                    }

                    // 判定のために読み込んだ prefab / テクスチャを解放する（候補はパス文字列のみ保持）
                    if (++scanned % ScanUnloadInterval == 0)
                    {
                        UnloadUnusedAssets();
                    }
                }

                UnloadUnusedAssets();

                if (candidates.Count == 0) return result;

                EnsureImportMeshes(candidates);

                for (int i = 0; i < candidates.Count; i++)
                {
                    Candidate candidate = candidates[i];
                    if (EditorUtility.DisplayCancelableProgressBar("SpriteAssist",
                            $"Migrating Mesh Prefab ({i + 1}/{candidates.Count})\n{candidate.meshPrefabPath}", (float)i / candidates.Count))
                    {
                        break;
                    }

                    if (Migrate(candidate.texturePath))
                    {
                        result.migrated++;
                    }
                    else
                    {
                        result.skipped++;
                    }

                    // dirty な prefab を都度ディスクへ書き出し、読み込んだアセットを解放する
                    if ((i + 1) % MigrateFlushInterval == 0)
                    {
                        AssetDatabase.SaveAssets();
                        UnloadUnusedAssets();
                    }
                }

                AssetDatabase.SaveAssets();
                return result;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                UnloadUnusedAssets();
            }
        }

        private static void UnloadUnusedAssets()
        {
            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        // 単一テクスチャの Mesh Prefab を新構造へリンクし直す。サブアセット Mesh が無ければ先に reimport する。
        public static bool Migrate(string texturePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (sprite == null || textureImporter == null || textureImporter.spriteImportMode != SpriteImportMode.Single) return false;

            // Mesh はテクスチャの reimport 成果物を使うため、dummy sprite（元画像のデコード）は不要
            using SpriteImportData importData = new SpriteImportData(sprite, textureImporter, texturePath, createDummySprite: false);
            if (!importData.HasMeshPrefab) return false;

            if (!SpriteMeshAssets.TryGetMeshes(texturePath, out _, out _))
            {
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                if (!SpriteMeshAssets.TryGetMeshes(texturePath, out _, out _))
                {
                    Debug.LogWarning($"[SpriteAssist] Mesh sub-asset was not generated for '{texturePath}'. Check inclusion settings.");
                    return false;
                }
            }

            SpriteConfigData configData = SpriteConfigData.GetData(textureImporter.userData);
            MeshCreatorBase meshCreator = MeshCreatorBase.GetInstance(configData.mode);
            MeshPrefabService.UpdateSubAssetsInMeshPrefab(importData, meshCreator, configData);
            AssetDatabase.WriteImportSettingsIfDirty(texturePath);
            return true;
        }

        private struct Candidate
        {
            public string texturePath;
            public string meshPrefabPath;
        }

        private static bool TryCreateCandidate(string assetPath, out Candidate candidate, out string skipReason)
        {
            candidate = default;
            skipReason = null;

            Type mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (mainType == typeof(GameObject))
            {
                return TryCreateCandidateFromPrefab(assetPath, out candidate, out skipReason);
            }

            if (typeof(Texture2D).IsAssignableFrom(mainType))
            {
                return TryCreateCandidateFromTexture(assetPath, out candidate, out skipReason);
            }

            return false;
        }

        private static bool TryCreateCandidateFromTexture(string texturePath, out Candidate candidate, out string skipReason)
        {
            candidate = default;
            skipReason = null;

            if (!SpriteAssistSettings.instance.ShouldProcessSprite(texturePath)) return false;

            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null || textureImporter.spriteImportMode != SpriteImportMode.Single) return false;
            if (!SpriteImportData.TryGetMeshPrefabPath(textureImporter, texturePath, out string meshPrefabPath)) return false;

            GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
            if (!SpriteMeshAssets.IsLegacyMeshPrefab(meshPrefab) && SpriteMeshAssets.IsLinkedToTexture(meshPrefab, texturePath)) return false;

            candidate = new Candidate { texturePath = texturePath, meshPrefabPath = meshPrefabPath };
            return true;
        }

        private static bool TryCreateCandidateFromPrefab(string meshPrefabPath, out Candidate candidate, out string skipReason)
        {
            candidate = default;
            skipReason = null;

            // t:GameObject には FBX 等も含まれるため、prefab 以外は対象にしない。
            if (!string.Equals(Path.GetExtension(meshPrefabPath), ".prefab", StringComparison.OrdinalIgnoreCase)) return false;

            GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
            if (!SpriteMeshAssets.IsLegacyMeshPrefab(meshPrefab)) return false;

            // Material のテクスチャから元スプライトを推定する（Inspector が MeshRenderer に対して行うのと同じ）
            Sprite sprite = SpriteUtil.FindSprite(meshPrefab);
            if (sprite == null)
            {
                if (SpriteMeshAssets.IsLegacyMeshPrefab(meshPrefab))
                {
                    skipReason = "cannot resolve the source sprite from the prefab's material";
                }

                return false;
            }

            string texturePath = AssetDatabase.GetAssetPath(sprite);
            if (!SpriteAssistSettings.instance.ShouldProcessSprite(texturePath))
            {
                skipReason = $"source texture '{texturePath}' is excluded by SpriteAssist settings";
                return false;
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null || textureImporter.spriteImportMode != SpriteImportMode.Single)
            {
                skipReason = $"source texture '{texturePath}' is not a Single sprite";
                return false;
            }

            if (!SpriteImportData.TryGetMeshPrefabPath(textureImporter, texturePath, out string linkedPrefabPath))
            {
                skipReason = $"source texture '{texturePath}' is not linked to this Mesh Prefab";
                return false;
            }

            if (!string.Equals(linkedPrefabPath, meshPrefabPath, StringComparison.OrdinalIgnoreCase))
            {
                skipReason = $"source texture '{texturePath}' is already linked to another Mesh Prefab '{linkedPrefabPath}'";
                return false;
            }

            candidate = new Candidate { texturePath = texturePath, meshPrefabPath = meshPrefabPath };
            return true;
        }

        // サブアセット Mesh が無いテクスチャをまとめて reimport する（1 件ずつ refresh させない）。
        // 数千件を 1 バッチにすると import 結果がまとめてメモリに載るため、一定件数ごとに区切って解放する。
        private static void EnsureImportMeshes(List<Candidate> candidates)
        {
            var missing = new List<string>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i % ScanUnloadInterval == 0)
                {
                    EditorUtility.DisplayProgressBar("SpriteAssist", $"Checking meshes ({i}/{candidates.Count})", (float)i / candidates.Count);
                }

                if (!SpriteMeshAssets.TryGetMeshes(candidates[i].texturePath, out _, out _))
                {
                    missing.Add(candidates[i].texturePath);
                }

                if ((i + 1) % ScanUnloadInterval == 0)
                {
                    UnloadUnusedAssets();
                }
            }

            UnloadUnusedAssets();

            if (missing.Count == 0) return;

            for (int start = 0; start < missing.Count; start += ReimportBatchSize)
            {
                int end = Math.Min(start + ReimportBatchSize, missing.Count);
                EditorUtility.DisplayProgressBar("SpriteAssist", $"Reimporting textures ({end}/{missing.Count})", (float)start / missing.Count);

                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = start; i < end; i++)
                    {
                        AssetDatabase.ImportAsset(missing[i], ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                UnloadUnusedAssets();
            }
        }

        private static IEnumerable<string> ExpandAssetPaths(IEnumerable<string> assetPaths)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var folders = new List<string>();

            foreach (string assetPath in assetPaths)
            {
                if (string.IsNullOrEmpty(assetPath)) continue;

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    folders.Add(assetPath);
                }
                else if (visited.Add(assetPath))
                {
                    yield return assetPath;
                }
            }

            if (folders.Count == 0) yield break;

            var guids = new List<string>();
            guids.AddRange(AssetDatabase.FindAssets("t:Texture2D", folders.ToArray()));
            guids.AddRange(AssetDatabase.FindAssets("t:GameObject", folders.ToArray()));

            for (int i = 0; i < guids.Count; i++)
            {
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("SpriteAssist", $"Scanning assets ({i}/{guids.Count})", (float)i / guids.Count);
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (visited.Add(assetPath))
                {
                    yield return assetPath;
                }
            }
        }

        private static string[] GetSelectedAssetPaths()
        {
            Object[] selection = Selection.GetFiltered<Object>(SelectionMode.Assets);
            var paths = new List<string>(selection.Length);

            foreach (Object obj in selection)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(path);
                }
            }

            return paths.ToArray();
        }
    }
}
