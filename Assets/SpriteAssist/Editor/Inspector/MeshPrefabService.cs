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
            meshCreator.OverrideGeometry(importData.sprite, importData.dummySprite, textureInfo, configData);
        }

        // import 中に呼ぶ。Mesh Prefab 用 Mesh をテクスチャ import の成果物として追加する。
        // identifier を固定しているので、prefab 側の MeshFilter 参照は reimport 後も維持される。
        public static void AddImportMeshes(AssetImportContext context, SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData)
        {
            TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
            meshCreator.CreateImportMeshes(importData.sprite, importData.dummySprite, textureInfo, configData, out Mesh rootMesh, out Mesh subMesh);

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

        // Mesh Prefab の構造・Material・Mesh 参照を更新する。
        // テクスチャの reimport 後（サブアセット Mesh が存在する状態）に呼ぶこと。
        // 旧構造（Mesh が prefab に埋め込み）の prefab は CleanUpSubAssets で埋め込み Mesh が除去され、新構造へ移行する。
        public static void UpdateSubAssetsInMeshPrefab(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData)
        {
            if (!importData.HasMeshPrefab) return;

            GameObject meshPrefab = importData.MeshPrefab;
            string meshPrefabPath = AssetDatabase.GetAssetPath(meshPrefab);
            TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
            PrefabUtil.CleanUpSubAssets(meshPrefab);
            meshCreator.UpdateExternalObject(meshPrefab, importData.sprite, textureInfo, configData);

            // prefab の root GameObject が差し替わった場合のみ remap し直す（不要な .meta 更新を避ける）
            GameObject savedMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
            if (savedMeshPrefab != null && importData.MeshPrefab != savedMeshPrefab)
            {
                importData.RemapExternalObject(savedMeshPrefab);
            }
        }

        public static bool IsLegacyMeshPrefab(SpriteImportData importData)
        {
            return importData.HasMeshPrefab && SpriteMeshAssets.IsLegacyMeshPrefab(importData.MeshPrefab);
        }
    }
}
