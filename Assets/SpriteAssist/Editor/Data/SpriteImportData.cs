using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteImportData
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
        {
            this.sprite = sprite;
            this.assetPath = assetPath;

            textureImporter = AssetImporter.GetAtPath(this.assetPath) as TextureImporter;
            textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            dummySprite = SpriteUtil.TryCreateDummySprite(sprite, textureImporter, assetPath);
            _oldSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), Path.GetFileNameWithoutExtension(assetPath));
            _newSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), MESH_PREFAB_IDENTIFIER);
            HasSpriteOutline = OutlineUtil.HasOutline(textureImporter);
        }

        public SpriteImportData(Sprite sprite, TextureImporter importer, string assetPath)
        {
            this.sprite = sprite;
            this.assetPath = assetPath;

            textureImporter = importer;
            textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            dummySprite = SpriteUtil.TryCreateDummySprite(sprite, textureImporter, assetPath);
            _oldSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), Path.GetFileNameWithoutExtension(assetPath));
            _newSourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), MESH_PREFAB_IDENTIFIER);
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
    }
}