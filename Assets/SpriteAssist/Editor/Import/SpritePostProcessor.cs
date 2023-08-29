using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePostProcessor : AssetPostprocessor
    {
        private void OnPostprocessSprites(Texture2D _, Sprite[] sprites)
        {
            bool isFirstAdded = AssetCreationChecker.currentAssetPath == assetPath;
            AssetCreationChecker.currentAssetPath = null;

            SpriteInspector.isSpriteReloaded = true;

            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter!.ReadTextureSettings(textureImporterSettings);

            if (textureImporterSettings.IsSingleSprite())
            {
                //override mesh(support first sprite only)
                foreach (var sprite in sprites)
                {
                    SpriteProcessor spriteProcessor = new SpriteProcessor(sprite, assetPath);
                    spriteProcessor.OverrideGeometry();
                    spriteProcessor.UpdateMeshInMeshPrefab();

                    if (isFirstAdded)
                    {
                        spriteProcessor.ClearOldMeshPrefabIfFound();
                    }
                    break;
                }
            }
            
            //auto rename
            if (SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically)
            {
                EditorApplication.delayCall += () =>
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    SpriteImportData importData = new SpriteImportData(sprite, assetPath);

                    if (importData.HasMeshPrefab)
                    {
                        PrefabUtil.TryRename(importData.assetPath, importData.MeshPrefab);
                    }
                };
            }
        }
    }

    public class AssetCreationChecker : AssetModificationProcessor
    {
        public static string currentAssetPath;

        static void OnWillCreateAsset(string assetPath)
        {
            if (assetPath.EndsWith(".meta"))
                return;

            currentAssetPath = assetPath;
        }
    }
}
