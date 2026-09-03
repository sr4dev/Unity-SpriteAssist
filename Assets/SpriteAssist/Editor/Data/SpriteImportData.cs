using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteImportData : IDisposable
    {
        public const string MESH_PREFAB_IDENTIFIER = "MeshPrefab";

        public readonly Sprite sprite;
        public readonly string assetPath;
        public readonly Sprite dummySprite;
        public readonly TextureImporter textureImporter;
        public readonly TextureImporterSettings textureImporterSettings;

        //TODO [Obsolete]
        private readonly AssetImporter.SourceAssetIdentifier _oldSourceAssetIdentifier;
        private readonly AssetImporter.SourceAssetIdentifier _newSourceAssetIdentifier;

        public bool IsTightMesh { get { return textureImporterSettings.spriteMeshType == SpriteMeshType.Tight; } }

        public bool HasMeshPrefab { get { return MeshPrefab != null; } }

        public GameObject MeshPrefab { get { return FindExternalObject() as GameObject; } }

        public bool HasSpriteOutline { get; private set; }

        public static bool TryGetSpriteImportData(Object obj, out SpriteImportData spriteImportData)
        {
            spriteImportData = null;

            Sprite sprite = SpriteUtil.FindSprite(obj);

            if (sprite == null)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null)
            {
                return false;
            }

            spriteImportData = new SpriteImportData(sprite, textureImporter, assetPath);
            return spriteImportData != null;
        }

        public SpriteImportData(Sprite sprite, string assetPath)
            : this(sprite, AssetImporter.GetAtPath(assetPath) as TextureImporter, assetPath)
        {
        }

        public SpriteImportData(Sprite sprite, TextureImporter importer, string assetPath)
            : this(sprite, importer, assetPath, createDummySprite: true)
        {
        }

        // createDummySprite=false: 元画像のデコード/リサイズを行わない（Mesh 生成が不要な処理向け。大量処理時のメモリ・時間を節約する）
        public SpriteImportData(Sprite sprite, TextureImporter importer, string assetPath, bool createDummySprite)
        {
            this.sprite = sprite;
            this.assetPath = assetPath;

            textureImporter = importer;
            textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            dummySprite = createDummySprite ? SpriteUtil.TryCreateDummySprite(sprite, textureImporter, assetPath) : null;
            _oldSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), Path.GetFileNameWithoutExtension(assetPath));
            _newSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), MESH_PREFAB_IDENTIFIER);
            HasSpriteOutline = OutlineUtil.HasOutline(textureImporter);
        }
        
        private Object FindExternalObject()
        {
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> map = textureImporter.GetExternalObjectMap();

            if (map.ContainsKey(_oldSourceAssetIdentifier))
                return map[_oldSourceAssetIdentifier];

            if (map.ContainsKey(_newSourceAssetIdentifier))
                return map[_newSourceAssetIdentifier];

            return null;
        }

        public void SetPrefabAsExternalObject(GameObject prefab, bool removeAssetToo)
        {
            if (removeAssetToo && MeshPrefab != null)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(MeshPrefab));
            RemapExternalObject(prefab);
        }

        public void RemapExternalObject(GameObject prefab)
        {
            textureImporter.RemoveRemap(_oldSourceAssetIdentifier);
            textureImporter.RemoveRemap(_newSourceAssetIdentifier);
            textureImporter.AddRemap(_newSourceAssetIdentifier, prefab);
            //textureImporter.SaveAndReimport();
        }

        public void RemoveExternalPrefab(bool removeAssetToo)
        {
            if (removeAssetToo && MeshPrefab != null)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(MeshPrefab));
            textureImporter.RemoveRemap(_oldSourceAssetIdentifier);
            textureImporter.RemoveRemap(_newSourceAssetIdentifier);
            //textureImporter.SaveAndReimport();
        }

        public void RemoveMissingExternalPrefab()
        {
            RemoveMissingExternalPrefab(textureImporter, assetPath);
        }

        // import worker では外部 prefab の Object を解決できないため、remap の key の有無だけで判定する
        public static bool HasMeshPrefabLink(TextureImporter textureImporter, string assetPath)
        {
            string legacyIdentifier = Path.GetFileNameWithoutExtension(assetPath);

            foreach (AssetImporter.SourceAssetIdentifier identifier in textureImporter.GetExternalObjectMap().Keys)
            {
                if (identifier.type == typeof(GameObject) &&
                    (identifier.name == MESH_PREFAB_IDENTIFIER || identifier.name == legacyIdentifier))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetMeshPrefabPath(TextureImporter textureImporter, string assetPath, out string meshPrefabPath)
        {
            string legacyIdentifier = Path.GetFileNameWithoutExtension(assetPath);

            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> externalObject in textureImporter.GetExternalObjectMap())
            {
                if ((externalObject.Key.name != MESH_PREFAB_IDENTIFIER && externalObject.Key.name != legacyIdentifier) ||
                    externalObject.Value is not GameObject prefab)
                {
                    continue;
                }

                meshPrefabPath = AssetDatabase.GetAssetPath(prefab);
                if (!string.IsNullOrEmpty(meshPrefabPath))
                {
                    return true;
                }
            }

            meshPrefabPath = null;
            return false;
        }

        public static bool RemoveMissingExternalPrefab(TextureImporter textureImporter, string assetPath)
        {
            var oldSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), Path.GetFileNameWithoutExtension(assetPath));
            var newSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), MESH_PREFAB_IDENTIFIER);
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> map = textureImporter.GetExternalObjectMap();
            bool removed = false;

            if (map.TryGetValue(oldSourceAssetIdentifier, out Object oldPrefab) && oldPrefab == null)
            {
                textureImporter.RemoveRemap(oldSourceAssetIdentifier);
                removed = true;
            }

            if (map.TryGetValue(newSourceAssetIdentifier, out Object newPrefab) && newPrefab == null)
            {
                textureImporter.RemoveRemap(newSourceAssetIdentifier);
                removed = true;
            }

            return removed;
        }

        public void Dispose()
        {
            if (dummySprite == null || dummySprite == sprite) return;

            Texture2D dummyTexture = dummySprite.texture;
            Object.DestroyImmediate(dummySprite);

            if (dummyTexture != null && dummyTexture != sprite.texture)
            {
                Object.DestroyImmediate(dummyTexture);
            }
        }
    }
}
