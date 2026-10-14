using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpriteAssist.Tests
{
    public class MeshPrefabCopyLinkerTests
    {
        private const string TempRoot = "Assets/SpriteAssistCopyLinkerTestTemp";
        private const string Folder = TempRoot + "/Src";
        private const string OwnerPath = Folder + "/Owner.png";
        private const string OwnerPrefabPath = Folder + "/Owner.prefab";
        private const string SourceTexturePath = "Assets/Example/Sprite/rebox-green-tri.png";
        private bool _renameSetting;

        [SetUp]
        public void SetUp()
        {
            _renameSetting = SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically;
            SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically = true;
            AssetDatabase.DeleteAsset(TempRoot);
            AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length));
            AssetDatabase.CreateFolder(TempRoot, "Src");
            Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, OwnerPath), Is.True);

            var importer = (TextureImporter)AssetImporter.GetAtPath(OwnerPath);
            var configData = new SpriteConfigData { mode = SpriteConfigData.Mode.TransparentMesh, transparentShaderName = "Unlit/Transparent", opaqueShaderName = "Unlit/Texture" };
            importer.userData = JsonUtility.ToJson(configData);
            AssetDatabase.WriteImportSettingsIfDirty(OwnerPath);
            AssetDatabase.ImportAsset(OwnerPath, ImportAssetOptions.ForceUpdate);
            importer = (TextureImporter)AssetImporter.GetAtPath(OwnerPath);
            using (var importData = new SpriteImportData(AssetDatabase.LoadAssetAtPath<Sprite>(OwnerPath), importer, OwnerPath, createDummySprite: false))
            {
                MeshCreatorBase meshCreator = MeshCreatorBase.GetInstance(configData.mode);
                MeshPrefabService.SetMeshPrefabContainer(importData, meshCreator, configData, removeOldMeshPrefab: false, attachedMeshPrefab: null);
                AssetDatabase.WriteImportSettingsIfDirty(OwnerPath);
                importer.SaveAndReimport();
                MeshPrefabService.UpdateSubAssetsInMeshPrefab(importData, meshCreator, configData);
                AssetDatabase.WriteImportSettingsIfDirty(OwnerPath);
            }
            AssetDatabase.SaveAssets();
            AssertLinked(OwnerPath, OwnerPrefabPath);
        }

        [TearDown]
        public void TearDown()
        {
            SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically = _renameSetting;
            AssetDatabase.DeleteAsset(TempRoot);
        }

        [UnityTest]
        public IEnumerator TextureOnlyCopy_DropsInheritedLink_AndKeepsOriginal()
        {
            const string copy = Folder + "/Copy.png";
            Assert.That(AssetDatabase.CopyAsset(OwnerPath, copy), Is.True);
            for (int i = 0; i < 60; i++) yield return null;

            var importer = (TextureImporter)AssetImporter.GetAtPath(copy);
            Assert.That(SpriteImportData.TryGetMeshPrefabPath(importer, copy, out _), Is.False);
            AssertLinked(OwnerPath, OwnerPrefabPath);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Copy.prefab"), Is.Null, "original prefab must not be renamed");
        }

        [UnityTest]
        public IEnumerator TextureAndPrefabCopy_LinksCopies()
        {
            const string copy = Folder + "/Copy.png";
            const string copyPrefab = Folder + "/Copy.prefab";
            Assert.That(AssetDatabase.CopyAssets(new[] { OwnerPath, OwnerPrefabPath }, new[] { copy, copyPrefab }), Is.True);
            for (int i = 0; i < 60; i++) yield return null;

            AssertLinked(copy, copyPrefab);
            AssertLinked(OwnerPath, OwnerPrefabPath);
        }

        [UnityTest]
        public IEnumerator FolderCopy_LinksCopies()
        {
            const string copyFolder = TempRoot + "/Dst";
            Assert.That(AssetDatabase.CopyAsset(Folder, copyFolder), Is.True);
            for (int i = 0; i < 60; i++) yield return null;

            AssertLinked(copyFolder + "/Owner.png", copyFolder + "/Owner.prefab");
            AssertLinked(OwnerPath, OwnerPrefabPath);
        }

        private static void AssertLinked(string texturePath, string prefabPath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            Assert.That(SpriteImportData.TryGetMeshPrefabPath(importer, texturePath, out string linked), Is.True, texturePath);
            Assert.That(linked, Is.EqualTo(prefabPath));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(SpriteMeshAssets.IsLinkedToTexture(prefab, texturePath), Is.True, prefabPath + " mesh");
            Assert.That(prefab.GetComponent<MeshRenderer>().sharedMaterial.mainTexture,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<Texture>(texturePath)), prefabPath + " material");
        }
    }
}
