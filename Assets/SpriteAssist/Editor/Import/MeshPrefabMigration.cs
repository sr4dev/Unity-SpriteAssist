using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    // 旧構造（Mesh が prefab に埋め込み）の Mesh Prefab を、テクスチャのサブアセット Mesh を参照する新構造へ移行する。
    public static class MeshPrefabMigration
    {
        [MenuItem("Assets/SpriteAssist/Migrate Legacy Mesh Prefabs", priority = 710)]
        private static void MigrateAllMenu()
        {
            int migrated = MigrateAll();
            EditorUtility.DisplayDialog("SpriteAssist", $"Migrated {migrated} Mesh Prefab(s).", "OK");
        }

        public static int MigrateAll()
        {
            List<string> texturePaths = FindLegacyLinkedTextures();
            if (texturePaths.Count == 0) return 0;

            try
            {
                EnsureImportMeshes(texturePaths);

                int migrated = 0;
                for (int i = 0; i < texturePaths.Count; i++)
                {
                    string texturePath = texturePaths[i];
                    if (EditorUtility.DisplayCancelableProgressBar("SpriteAssist", $"Migrating Mesh Prefab ({i + 1}/{texturePaths.Count})\n{texturePath}", (float)i / texturePaths.Count))
                    {
                        break;
                    }

                    if (Migrate(texturePath))
                    {
                        migrated++;
                    }
                }

                AssetDatabase.SaveAssets();
                return migrated;
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

        private static List<string> FindLegacyLinkedTextures()
        {
            var result = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });

            for (int i = 0; i < guids.Length; i++)
            {
                string texturePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!SpriteAssistSettings.instance.ShouldProcessSprite(texturePath)) continue;

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("SpriteAssist", $"Scanning textures ({i}/{guids.Length})", (float)i / guids.Length);
                }

                TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (textureImporter == null || textureImporter.spriteImportMode != SpriteImportMode.Single) continue;
                if (!SpriteImportData.TryGetMeshPrefabPath(textureImporter, texturePath, out string meshPrefabPath)) continue;

                GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
                if (SpriteMeshAssets.IsLegacyMeshPrefab(meshPrefab) || !SpriteMeshAssets.IsLinkedToTexture(meshPrefab, texturePath))
                {
                    result.Add(texturePath);
                }
            }

            EditorUtility.ClearProgressBar();
            return result;
        }

        // サブアセット Mesh が無いテクスチャをまとめて reimport する（1 件ずつ refresh させない）
        private static void EnsureImportMeshes(List<string> texturePaths)
        {
            var missing = new List<string>();
            foreach (string texturePath in texturePaths)
            {
                if (!SpriteMeshAssets.TryGetMeshes(texturePath, out _, out _))
                {
                    missing.Add(texturePath);
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
    }
}
