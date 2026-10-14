using System;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace SpriteAssist
{
    public static class MeshPrefabService
    {
        public static void OverrideGeometry(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData)
        {
            TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
            if (SpriteConfigData.IsUnityDefaultMode(configData.mode))
            {
                importData.defaultMeshGeometry = DefaultMeshGeometry.Generate(importData.sprite, importData.dummySprite, textureInfo, configData);
                importData.defaultMeshGeometry.ApplyToSprite(importData.sprite, textureInfo);
                return;
            }
            meshCreator.OverrideGeometry(importData.sprite, importData.dummySprite, textureInfo, configData);
        }

        // Called during import. Add Mesh Prefab Meshes as texture import artifacts.
        // Stable identifiers preserve the prefab's MeshFilter references across reimports.
        public static void AddImportMeshes(AssetImportContext context, SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData)
        {
            TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
            Mesh rootMesh, subMesh;
            if (importData.defaultMeshGeometry != null)
            {
                rootMesh = importData.defaultMeshGeometry.CreateMesh(TextureInfo.ForSourceImage(importData.sprite, importData.assetPath), configData);
                subMesh = null;
            }
            else meshCreator.CreateImportMeshes(importData.sprite, importData.dummySprite, textureInfo, configData, out rootMesh, out subMesh);

            if (rootMesh == null || (configData.mode == SpriteConfigData.Mode.ComplexMesh && subMesh == null))
            {
                if (rootMesh != null) UnityEngine.Object.DestroyImmediate(rootMesh);
                if (subMesh != null) UnityEngine.Object.DestroyImmediate(subMesh);
                throw new InvalidOperationException($"[SpriteAssist] Required Mesh was not generated for '{importData.assetPath}' ({configData.mode}).");
            }

            if (rootMesh != null)
            {
                context.AddObjectToAsset(SpriteMeshAssets.ROOT_MESH_IDENTIFIER, rootMesh);
            }

            if (subMesh != null)
            {
                context.AddObjectToAsset(SpriteMeshAssets.SUB_MESH_IDENTIFIER, subMesh);
            }
        }

        public static void SetMeshPrefabContainer(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData, bool removeOldMeshPrefab, GameObject attachedMeshPrefab)
        {
            importData.RemoveExternalPrefab(removeOldMeshPrefab);

            TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
            GameObject prefab = attachedMeshPrefab != null ? attachedMeshPrefab : meshCreator.CreateExternalObject(importData.sprite, textureInfo, configData);
            importData.SetPrefabAsExternalObject(prefab, removeOldMeshPrefab);
        }

        public static void RemoveMeshPrefabContainer(SpriteImportData importData, bool removeOldMeshPrefabToo)
        {
            importData.RemoveExternalPrefab(removeOldMeshPrefabToo);
        }

        // Update Mesh Prefab structure and Mesh references.
        // Call after texture reimport, once Mesh subassets exist.
        // CleanUpSubAssets removes embedded Meshes from legacy prefabs, migrating them to the new format.
        // Preserve existing Layer / Tag / Sorting / Material values; apply defaults only when adding a new MeshRenderer.
        public static bool UpdateSubAssetsInMeshPrefab(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData, bool legacyInputsValidated = false, GameObject meshPrefabOverride = null)
        {
            GameObject meshPrefab = meshPrefabOverride != null ? meshPrefabOverride : importData.MeshPrefab;
            if (meshPrefab == null) return false;

            // The legacy Mesh may be the only source of correct outlines and UVs. Keep it until validated recovery completes.
            if (!legacyInputsValidated && SpriteConfigData.IsUnityDefaultMode(configData.mode) && SpriteMeshAssets.IsLegacyMeshPrefab(meshPrefab))
                throw new InvalidOperationException("Legacy Default Mesh requires validated outline/UV migration before removing embedded meshes.");

            // Keep existing references, hierarchy, and legacy subassets unchanged until all required Meshes are ready.
            if (!SpriteMeshAssets.TryGetMeshes(importData.assetPath, out Mesh rootMesh, out Mesh subMesh) ||
                rootMesh == null || (configData.mode == SpriteConfigData.Mode.ComplexMesh && subMesh == null))
            {
                Debug.LogWarning($"[SpriteAssist] Required Mesh sub-assets are missing for '{importData.assetPath}' ({configData.mode}). Mesh Prefab was not updated.");
                return false;
            }

            string meshPrefabPath = AssetDatabase.GetAssetPath(meshPrefab);
            TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
            PrefabUtil.CleanUpSubAssets(meshPrefab);
            meshCreator.UpdateExternalObject(meshPrefab, importData.sprite, textureInfo, configData);
            // Also clean up after updating: hierarchy reconstruction (Single <-> Complex) can orphan Materials from removed child Renderers.
            PrefabUtil.CleanUpSubAssets(meshPrefab);

            // Write each updated prefab to disk individually.
            AssetDatabase.SaveAssetIfDirty(meshPrefab);

            // Remap only if the prefab's root GameObject was replaced, avoiding unnecessary .meta updates.
            GameObject savedMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
            if (meshPrefabOverride == null && savedMeshPrefab != null && importData.MeshPrefab != savedMeshPrefab)
            {
                importData.RemapExternalObject(savedMeshPrefab);
            }

            return true;
        }

        public static bool IsLegacyMeshPrefab(SpriteImportData importData)
        {
            return importData.HasMeshPrefab && SpriteMeshAssets.IsLegacyMeshPrefab(importData.MeshPrefab);
        }
    }
}
