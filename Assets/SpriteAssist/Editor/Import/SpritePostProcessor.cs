using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    // Output Mesh Prefab Meshes as texture import artifacts (texture subassets).
    // Mesh updates follow Unity's import dependencies. Do not rewrite prefab files during import.
    public class SpritePostProcessor : AssetPostprocessor
    {
        // Increment whenever the import output (Mesh subassets) changes to invalidate old artifacts.
        // v2: generate outline source textures on the CPU instead of GPU Blit/ReadPixels, invalidating rectangular Mesh artifacts from -nographics.
        // v3 was used to test Mesh visibility. v4: output Meshes even without a linked prefab and invalidate old artifacts.
        // v5: invalidate artifacts after fixing iShape winding settings and touching-hole handling.
        // v6: invalidate artifacts that mistook separate islands touching the boundary for holes.
        // Regenerate persistent Meshes from source-image outline coordinates and recovered UVs.
        private const uint VERSION = 7;

        private const int MaxRenameAttempts = 3;

        // key: Mesh Prefab GUID, value: sprite asset path
        private static readonly Dictionary<string, string> _pendingRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _renameAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool _renameScheduled;

        public override uint GetVersion()
        {
            return VERSION;
        }

        private void OnPostprocessSprites(Texture2D tex, Sprite[] sprites)
        {
            if (!SpriteAssistSettings.instance.ShouldProcessSprite(assetPath)) return;

            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter!.ReadTextureSettings(textureImporterSettings);

            if (!textureImporterSettings.IsSingleSprite()) return;

            SpriteImportData importData = null;
            try
            {
                if (!TryResolveFirstSprite(sprites, out importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData)) return;

                // Override the geometry of the Sprite being imported.
                MeshPrefabService.OverrideGeometry(importData, meshCreator, configData);

                // Always output Meshes required by the current mode, regardless of prefab links or previous import results.
                MeshPrefabService.AddImportMeshes(context, importData, meshCreator, configData);
            }
            catch (Exception e)
            {
                // Silently succeeding with Unity's default rectangular geometry would spread corrupt artifacts to all machines through Accelerator.
                // Fail explicitly with an import error and log the cause.
                context.LogImportError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                importData?.Dispose();
            }
        }

        // Called in the main process after import. Modify prefab contents only when retargeting immediately after duplication.
        // Clean remaps for deleted prefabs, resolve remaps inherited by copies, and optionally rename prefabs automatically.
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (PrefabUtil.IsAssetImportWorkerProcess()) return;

            MeshPrefabCopyLinker.CollectPrefabs(importedAssets);

            bool renameAutomatically = SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically;

            foreach (string importedAssetPath in importedAssets)
            {
                if (!SpriteAssistSettings.instance.ShouldProcessSprite(importedAssetPath)) continue;

                TextureImporter textureImporter = AssetImporter.GetAtPath(importedAssetPath) as TextureImporter;
                if (textureImporter == null) continue;

                if (textureImporter.spriteImportMode != SpriteImportMode.Single) continue;

                SpriteInspector.isSpriteReloaded = true;

                if (SpriteImportData.RemoveMissingExternalPrefab(textureImporter, importedAssetPath))
                {
                    AssetDatabase.WriteImportSettingsIfDirty(importedAssetPath);
                }

                // Correct ownership before renaming. If a prefab using another image's Mesh becomes a rename candidate,
                // multiple images compete for the same prefab, causing its name to oscillate with import order.
                if (MeshPrefabCopyLinker.Defer(textureImporter, importedAssetPath)) continue;

                if (!renameAutomatically) continue;
                if (!SpriteImportData.TryGetMeshPrefabPath(textureImporter, importedAssetPath, out string meshPrefabPath)) continue;
                if (Path.GetFileNameWithoutExtension(meshPrefabPath) == Path.GetFileNameWithoutExtension(importedAssetPath)) continue;

                // Store GUIDs because paths may change before the flush.
                string meshPrefabGuid = AssetDatabase.AssetPathToGUID(meshPrefabPath);
                if (string.IsNullOrEmpty(meshPrefabGuid)) continue;

                _pendingRenames[meshPrefabGuid] = importedAssetPath;
            }

            ScheduleRename();
        }

        private bool TryResolveFirstSprite(Sprite[] sprites, out SpriteImportData importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData)
        {
            // Process only the first Sprite, preserving existing behavior.
            foreach (var sprite in sprites)
            {
                importData = new SpriteImportData(sprite, assetPath);
                configData = SpriteConfigData.GetData(importData.textureImporter.userData);
                meshCreator = MeshCreatorBase.GetInstance(configData.mode);
                return true;
            }

            importData = null;
            meshCreator = null;
            configData = null;
            return false;
        }

        private static void ScheduleRename()
        {
            if (_renameScheduled || _pendingRenames.Count == 0) return;

            _renameScheduled = true;
            EditorApplication.delayCall += FlushRenames;
        }

        private static void FlushRenames()
        {
            _renameScheduled = false;
            if (_pendingRenames.Count == 0) return;

            var renames = new List<KeyValuePair<string, string>>(_pendingRenames);
            _pendingRenames.Clear();

            // Rename individually because batching with StartAssetEditing can desynchronize subsequent moves and AssetDatabase state.
            // Assets moved immediately beforehand may not be renameable in the same frame, so retry failures next frame.
            foreach (KeyValuePair<string, string> rename in renames)
            {
                if (TryRename(rename.Key, rename.Value, out string error))
                {
                    _renameAttempts.Remove(rename.Key);
                    continue;
                }

                _renameAttempts.TryGetValue(rename.Key, out int attempts);
                if (attempts + 1 >= MaxRenameAttempts)
                {
                    _renameAttempts.Remove(rename.Key);
                    Debug.LogWarning($"[SpriteAssist] Failed to rename Mesh Prefab '{AssetDatabase.GUIDToAssetPath(rename.Key)}': {error}");
                    continue;
                }

                _renameAttempts[rename.Key] = attempts + 1;
                _pendingRenames[rename.Key] = rename.Value;
            }

            ScheduleRename();
        }

        private static bool TryRename(string meshPrefabGuid, string spriteAssetPath, out string error)
        {
            error = null;
            string meshPrefabPath = AssetDatabase.GUIDToAssetPath(meshPrefabGuid);
            if (string.IsNullOrEmpty(meshPrefabPath))
            {
                error = "Mesh Prefab not found";
                return false;
            }

            // Immediately after image duplication, the old prefab remap remains. Do not rename a prefab owned by another image.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
            if (!SpriteMeshAssets.IsLinkedToTexture(prefab, spriteAssetPath) &&
                AssetDatabase.GetAssetPath(SpriteUtil.FindSprite(prefab)) != spriteAssetPath) return true;

            string spriteName = Path.GetFileNameWithoutExtension(spriteAssetPath);
            if (Path.GetFileNameWithoutExtension(meshPrefabPath) == spriteName) return true;

            error = AssetDatabase.RenameAsset(meshPrefabPath, spriteName);
            return string.IsNullOrEmpty(error);
        }
    }
}
