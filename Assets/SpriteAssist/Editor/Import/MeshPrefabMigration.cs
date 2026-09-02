using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    // 旧構造（Mesh が prefab に埋め込み）の Mesh Prefab を、テクスチャのサブアセット Mesh を参照する新構造へ移行する。
    // Project ウィンドウで prefab / テクスチャ / フォルダを選択していればその範囲、未選択ならプロジェクト全体を対象にする。
    public static class MeshPrefabMigration
    {
        public struct Result
        {
            public int migrated;
            public int linked;
            public int skipped;
        }

        private const string MigrateSelectedMenu = "Assets/SpriteAssist/Migrate Legacy Mesh Prefabs (Selected)";
        private const string MigrateAllMenu = "Assets/SpriteAssist/Migrate Legacy Mesh Prefabs (Entire Project)";

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
                $"Mesh Prefab migration ({scope})\n\nMigrated: {result.migrated}\nNewly linked: {result.linked}{skipped}", "OK");
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
                }

                if (candidates.Count == 0) return result;

                // 未リンクの prefab はテクスチャに remap を追加してから reimport する
                foreach (Candidate candidate in candidates)
                {
                    if (candidate.needsLink)
                    {
                        Link(candidate);
                        result.linked++;
                    }
                }

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
                }

                AssetDatabase.SaveAssets();
                return result;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // 単一テクスチャの Mesh Prefab を新構造へリンクし直す。サブアセット Mesh が無ければ先に reimport する。
        public static bool Migrate(string texturePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (sprite == null || textureImporter == null || textureImporter.spriteImportMode != SpriteImportMode.Single) return false;

            using SpriteImportData importData = new SpriteImportData(sprite, textureImporter, texturePath);
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
            public bool needsLink;
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

            GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
            if (meshPrefab == null || meshPrefab.GetComponent<MeshFilter>() == null) return false;

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

            bool needsLink = true;
            if (SpriteImportData.TryGetMeshPrefabPath(textureImporter, texturePath, out string linkedPrefabPath))
            {
                if (!string.Equals(linkedPrefabPath, meshPrefabPath, StringComparison.OrdinalIgnoreCase))
                {
                    skipReason = $"source texture '{texturePath}' is already linked to another Mesh Prefab '{linkedPrefabPath}'";
                    return false;
                }

                needsLink = false;
            }

            if (!needsLink && !SpriteMeshAssets.IsLegacyMeshPrefab(meshPrefab) && SpriteMeshAssets.IsLinkedToTexture(meshPrefab, texturePath)) return false;

            candidate = new Candidate { texturePath = texturePath, meshPrefabPath = meshPrefabPath, needsLink = needsLink };
            return true;
        }

        private static void Link(Candidate candidate)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(candidate.texturePath) as TextureImporter;
            GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidate.meshPrefabPath);
            if (textureImporter == null || meshPrefab == null) return;

            textureImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER), meshPrefab);
            EditorUtility.SetDirty(textureImporter);
            AssetDatabase.WriteImportSettingsIfDirty(candidate.texturePath);
            Debug.Log($"[SpriteAssist] Linked Mesh Prefab '{candidate.meshPrefabPath}' to '{candidate.texturePath}'.");
        }

        // サブアセット Mesh が無いテクスチャをまとめて reimport する（1 件ずつ refresh させない）
        private static void EnsureImportMeshes(List<Candidate> candidates)
        {
            var missing = new List<string>();
            foreach (Candidate candidate in candidates)
            {
                if (candidate.needsLink || !SpriteMeshAssets.TryGetMeshes(candidate.texturePath, out _, out _))
                {
                    missing.Add(candidate.texturePath);
                }
            }

            if (missing.Count == 0) return;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string texturePath in missing)
                {
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
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
