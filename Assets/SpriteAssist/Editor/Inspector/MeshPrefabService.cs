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

        // import 中に呼ぶ。Mesh Prefab 用 Mesh をテクスチャ import の成果物として追加する。
        // identifier を固定しているので、prefab 側の MeshFilter 参照は reimport 後も維持される。
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

        // Mesh Prefab の構造・Mesh 参照を更新する。
        // テクスチャの reimport 後（サブアセット Mesh が存在する状態）に呼ぶこと。
        // 旧構造（Mesh が prefab に埋め込み）の prefab は CleanUpSubAssets で埋め込み Mesh が除去され、新構造へ移行する。
        // 既存の Layer / Tag / Sorting / Material は保持し、MeshRenderer を新規追加する場合のみ初期値を適用する。
        public static bool UpdateSubAssetsInMeshPrefab(SpriteImportData importData, MeshCreatorBase meshCreator, SpriteConfigData configData, bool legacyInputsValidated = false, GameObject meshPrefabOverride = null)
        {
            GameObject meshPrefab = meshPrefabOverride != null ? meshPrefabOverride : importData.MeshPrefab;
            if (meshPrefab == null) return false;

            // 旧 Mesh は輪郭と UV の唯一の正解である場合がある。検証済み復元を経るまで破棄しない。
            if (!legacyInputsValidated && SpriteConfigData.IsUnityDefaultMode(configData.mode) && SpriteMeshAssets.IsLegacyMeshPrefab(meshPrefab))
                throw new InvalidOperationException("Legacy Default Mesh requires validated outline/UV migration before removing embedded meshes.");

            // 必要な Mesh が揃うまでは、既存参照・階層・legacy subasset を変更しない。
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
            // 階層の再構築（Single ⇔ Complex）で消えた子 Renderer の Material が孤立するため、更新後にも除去する
            PrefabUtil.CleanUpSubAssets(meshPrefab);

            // 更新済み prefab を 1 件ごとに書き出す。
            AssetDatabase.SaveAssetIfDirty(meshPrefab);

            // prefab の root GameObject が差し替わった場合のみ remap し直す（不要な .meta 更新を避ける）
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
