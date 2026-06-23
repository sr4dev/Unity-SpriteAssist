using UnityEngine;

namespace SpriteAssist
{
    public static class MeshPrefabService
    {
        public static void OverrideGeometry(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData)
        {
            TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
            meshCreator.OverrideGeometry(importData.sprite, importData.dummySprite, textureInfo, configData);
        }

        public static void UpdateMeshInMeshPrefab(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData)
        {
            if (importData.HasMeshPrefab)
            {
                TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
                meshCreator.UpdateMeshInMeshPrefab(importData.MeshPrefab, importData.sprite, importData.dummySprite, textureInfo, configData);
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

        public static void UpdateSubAssetsInMeshPrefab(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData)
        {
            if (importData.HasMeshPrefab)
            {
                TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
                PrefabUtil.CleanUpSubAssets(importData.MeshPrefab);
                meshCreator.UpdateExternalObject(importData.MeshPrefab, importData.sprite, importData.dummySprite, textureInfo, configData);
                importData.RemapExternalObject(importData.MeshPrefab);
            }
        }
    }
}
