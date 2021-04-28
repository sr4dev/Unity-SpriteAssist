using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteImportData
    {
        public readonly Sprite sprite;
        public readonly string assetPath;
        public readonly TextureImporter textureImporter;
        public readonly TextureImporterSettings textureImporterSettings;
        public readonly AssetImporter.SourceAssetIdentifier sourceAssetIdentifier;

        public bool IsTightMesh { get { return textureImporterSettings.spriteMeshType == SpriteMeshType.Tight; } }

        public bool HasMeshPrefab { get { return MeshPrefab != null; } }

        public GameObject MeshPrefab { get { return FindExternalObject() as GameObject; } }

        public SpriteImportData(Sprite sprite, string assetPath)
        {
            this.sprite = sprite;
            this.assetPath = assetPath;

            textureImporter = AssetImporter.GetAtPath(this.assetPath) as TextureImporter;
            textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            sourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), sprite.texture.name);
        }

        public SpriteImportData(Sprite sprite, TextureImporter importer, string assetPath)
        {
            this.sprite = sprite;
            this.assetPath = assetPath;

            textureImporter = importer;
            textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            sourceAssetIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(GameObject), sprite.texture.name);
        }
        
        private Object FindExternalObject()
        {
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> map = textureImporter.GetExternalObjectMap();
            return map.ContainsKey(sourceAssetIdentifier) ? map[sourceAssetIdentifier] : null;
        }

        public void SetPrefabAsExternalObject(GameObject prefab)
        {
            if (MeshPrefab != null)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(MeshPrefab));
            RemapExternalObject(prefab);
        }

        public void RemapExternalObject(GameObject prefab)
        {
            textureImporter.RemoveRemap(sourceAssetIdentifier);
            textureImporter.AddRemap(sourceAssetIdentifier, prefab);
            textureImporter.SaveAndReimport();
        }

        public void RemoveExternalPrefab()
        {
            if (MeshPrefab != null)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(MeshPrefab));
            textureImporter.RemoveRemap(sourceAssetIdentifier);
            textureImporter.SaveAndReimport();
        }
    }
}