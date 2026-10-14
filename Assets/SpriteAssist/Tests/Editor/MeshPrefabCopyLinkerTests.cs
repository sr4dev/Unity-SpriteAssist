using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
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

        [UnityTest]
        public IEnumerator RepeatedImports_InBothOrders_DoNotOscillateNames()
        {
            const string copy = Folder + "/Copy.png";
            const string copyPrefab = Folder + "/Copy.prefab";
            Assert.That(AssetDatabase.CopyAssets(new[] { OwnerPath, OwnerPrefabPath }, new[] { copy, copyPrefab }), Is.True);
            for (int i = 0; i < 60; i++) yield return null;
            AssertLinked(copy, copyPrefab);
            string originalGuid = AssetDatabase.AssetPathToGUID(OwnerPrefabPath);
            string copyGuid = AssetDatabase.AssetPathToGUID(copyPrefab);
            byte[] originalBytes = File.ReadAllBytes(OwnerPrefabPath);
            byte[] copyBytes = File.ReadAllBytes(copyPrefab);
            for (int round = 0; round < 6; round++)
            {
                string first = round % 2 == 0 ? OwnerPath : copy;
                string second = round % 2 == 0 ? copy : OwnerPath;
                AssetDatabase.ImportAsset(first, ImportAssetOptions.ForceUpdate);
                for (int frame = 0; frame < 5; frame++) yield return null;
                AssetDatabase.ImportAsset(second, ImportAssetOptions.ForceUpdate);
                for (int frame = 0; frame < 5; frame++) yield return null;
                AssetDatabase.Refresh();
                for (int frame = 0; frame < 5; frame++) yield return null;
                Assert.That(AssetDatabase.GUIDToAssetPath(originalGuid), Is.EqualTo(OwnerPrefabPath), "original name, round " + round);
                Assert.That(AssetDatabase.GUIDToAssetPath(copyGuid), Is.EqualTo(copyPrefab), "copy name, round " + round);
                AssertLinked(OwnerPath, OwnerPrefabPath);
                AssertLinked(copy, copyPrefab);
                Assert.That(File.ReadAllBytes(OwnerPrefabPath), Is.EqualTo(originalBytes));
                Assert.That(File.ReadAllBytes(copyPrefab), Is.EqualTo(copyBytes));
            }
        }

        [UnityTest]
        public IEnumerator ExistingDuplicate_ReimportClearsLink_WithoutRenamingOriginal()
        {
            const string copy = Folder + "/Copy.png";
            Assert.That(AssetDatabase.CopyAsset(OwnerPath, copy), Is.True);
            for (int frame = 0; frame < 10; frame++) yield return null;
            var importer = AssetImporter.GetAtPath(copy);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER),
                AssetDatabase.LoadAssetAtPath<GameObject>(OwnerPrefabPath));
            AssetDatabase.WriteImportSettingsIfDirty(copy);
            string guid = AssetDatabase.AssetPathToGUID(OwnerPrefabPath);
            for (int round = 0; round < 4; round++)
            {
                AssetDatabase.ImportAsset(round % 2 == 0 ? copy : OwnerPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(round % 2 == 0 ? OwnerPath : copy, ImportAssetOptions.ForceUpdate);
                for (int frame = 0; frame < 10; frame++) yield return null;
                Assert.That(SpriteImportData.TryGetMeshPrefabPath((TextureImporter)AssetImporter.GetAtPath(copy), copy, out _), Is.False);
                Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.EqualTo(OwnerPrefabPath));
                AssertLinked(OwnerPath, OwnerPrefabPath);
            }
        }

        [UnityTest]
        public IEnumerator ExistingPrefabReimport_MustNotBeTreatedAsCopy()
        {
            const string existing = Folder + "/Existing.prefab";
            const string copy = Folder + "/Copy.png";
            Assert.That(AssetDatabase.CopyAsset(OwnerPrefabPath, existing), Is.True);
            for (int frame = 0; frame < 10; frame++) yield return null;
            byte[] existingBytes = File.ReadAllBytes(existing);
            Assert.That(AssetDatabase.CopyAsset(OwnerPath, copy), Is.True);
            AssetDatabase.ImportAsset(existing, ImportAssetOptions.ForceUpdate);
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(File.ReadAllBytes(existing), Is.EqualTo(existingBytes), "An existing prefab was retargeted as if it were a new copy");
            Assert.That(SpriteImportData.TryGetMeshPrefabPath((TextureImporter)AssetImporter.GetAtPath(copy), copy, out _), Is.False);
            AssertLinked(OwnerPath, OwnerPrefabPath);
        }

        [UnityTest]
        public IEnumerator QueuedRename_MeshOwnerChanged_MustIgnoreMaterialOwner()
        {
            const string copy = Folder + "/Copy.png";
            const string candidate = Folder + "/Unrenamed.prefab";
            Assert.That(AssetDatabase.CopyAsset(OwnerPath, copy), Is.True);
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(AssetDatabase.CopyAsset(OwnerPrefabPath, candidate), Is.True);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidate);
            Assert.That(SpriteMeshAssets.TryGetMeshes(copy, out Mesh copyMesh, out _), Is.True);
            prefab.GetComponent<MeshFilter>().sharedMesh = copyMesh;
            prefab.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(copy);
            EditorUtility.SetDirty(prefab.GetComponent<MeshRenderer>().sharedMaterial);
            PrefabUtility.SavePrefabAsset(prefab);
            AssetDatabase.SaveAssets();
            var importer = AssetImporter.GetAtPath(copy);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER), prefab);
            AssetDatabase.WriteImportSettingsIfDirty(copy);
            AssetDatabase.ImportAsset(copy, ImportAssetOptions.ForceUpdate);
            string guid = AssetDatabase.AssetPathToGUID(candidate);
            var queue = (System.Collections.Generic.Dictionary<string, string>)typeof(SpritePostProcessor)
                .GetField("_pendingRenames", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Assert.That(queue.ContainsKey(guid), Is.True, "A valid rename must have been queued first");
            Assert.That(SpriteMeshAssets.TryGetMeshes(OwnerPath, out Mesh ownerMesh, out _), Is.True);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidate);
            prefab.GetComponent<MeshFilter>().sharedMesh = ownerMesh;
            PrefabUtility.SavePrefabAsset(prefab);
            typeof(SpritePostProcessor).GetMethod("FlushRenames", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.EqualTo(candidate), "Material fallback renamed a prefab whose mesh belongs to another texture");
        }

        private static void DuplicateInProjectWindow(params string[] paths)
        {
            var previous = Selection.objects;
            try
            {
                Selection.objects = paths.Select(AssetDatabase.LoadMainAssetAtPath).ToArray();
                EditorUtility.FocusProjectWindow();
                var clipboard = System.AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("UnityEditor.AssetClipboardUtility")).FirstOrDefault(type => type != null);
                Assert.That(clipboard, Is.Not.Null, "Unity's Project-window clipboard type changed");
                var command = clipboard.GetMethod("DuplicateSelectedAssets", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, System.Type.EmptyTypes, null);
                Assert.That(command, Is.Not.Null, "Unity's Project-window duplicate command changed");
                command.Invoke(null, null);
            }
            finally { Selection.objects = previous; }
        }

        [UnityTest]
        public IEnumerator ProjectDuplicate_TextureOnly_DropsInheritedLink()
        {
            var before = AssetDatabase.FindAssets("t:Texture2D", new[] { TempRoot });
            DuplicateInProjectWindow(OwnerPath);
            string copy = AssetDatabase.FindAssets("t:Texture2D", new[] { TempRoot }).Except(before).Select(AssetDatabase.GUIDToAssetPath).Single();
            var importer = (TextureImporter)AssetImporter.GetAtPath(copy);
            Assert.That(SpriteImportData.TryGetMeshPrefabPath(importer, copy, out string inherited), Is.True, "Native duplication must inherit the link before repair");
            Assert.That(inherited, Is.EqualTo(OwnerPrefabPath));
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(SpriteImportData.TryGetMeshPrefabPath((TextureImporter)AssetImporter.GetAtPath(copy), copy, out _), Is.False);
            AssertLinked(OwnerPath, OwnerPrefabPath);
        }

        [UnityTest]
        public IEnumerator ProjectDuplicate_PairedAssets_InBothSelectionOrders()
        {
            for (int order = 0; order < 2; order++)
            {
                var texturesBefore = AssetDatabase.FindAssets("t:Texture2D", new[] { TempRoot });
                var prefabsBefore = AssetDatabase.FindAssets("t:Prefab", new[] { TempRoot });
                DuplicateInProjectWindow(order == 0 ? new[] { OwnerPath, OwnerPrefabPath } : new[] { OwnerPrefabPath, OwnerPath });
                string texture = AssetDatabase.FindAssets("t:Texture2D", new[] { TempRoot }).Except(texturesBefore).Select(AssetDatabase.GUIDToAssetPath).Single();
                string prefab = AssetDatabase.FindAssets("t:Prefab", new[] { TempRoot }).Except(prefabsBefore).Select(AssetDatabase.GUIDToAssetPath).Single();
                for (int frame = 0; frame < 10; frame++) yield return null;
                AssertLinked(texture, prefab);
                AssertLinked(OwnerPath, OwnerPrefabPath);
            }
        }

        [UnityTest]
        public IEnumerator ProjectDuplicate_Folder_LinksCopies()
        {
            var texturesBefore = AssetDatabase.FindAssets("t:Texture2D", new[] { TempRoot });
            var prefabsBefore = AssetDatabase.FindAssets("t:Prefab", new[] { TempRoot });
            DuplicateInProjectWindow(Folder);
            string texture = AssetDatabase.FindAssets("t:Texture2D", new[] { TempRoot }).Except(texturesBefore).Select(AssetDatabase.GUIDToAssetPath).Single();
            string prefab = AssetDatabase.FindAssets("t:Prefab", new[] { TempRoot }).Except(prefabsBefore).Select(AssetDatabase.GUIDToAssetPath).Single();
            for (int frame = 0; frame < 10; frame++) yield return null;
            AssertLinked(texture, prefab);
            AssertLinked(OwnerPath, OwnerPrefabPath);
        }

        [UnityTest]
        public IEnumerator SeparateDuplications_DoNotReuseEarlierPrefab()
        {
            const string earlier = Folder + "/Earlier.prefab";
            const string texture = Folder + "/Later.png";
            AssetDatabase.CopyAsset(OwnerPrefabPath, earlier);
            for (int frame = 0; frame < 10; frame++) yield return null;
            byte[] before = File.ReadAllBytes(earlier);
            AssetDatabase.CopyAsset(OwnerPath, texture);
            AssetDatabase.ImportAsset(earlier, ImportAssetOptions.ForceUpdate);
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(File.ReadAllBytes(earlier), Is.EqualTo(before));
            Assert.That(SpriteImportData.TryGetMeshPrefabPath((TextureImporter)AssetImporter.GetAtPath(texture), texture, out _), Is.False);
        }

        [UnityTest]
        public IEnumerator AmbiguousCopiedPrefabs_AreNotChosenByPathProximity()
        {
            const string texture = Folder + "/Near.png";
            const string near = Folder + "/Near.prefab";
            const string far = TempRoot + "/Far.prefab";
            LogAssert.Expect(LogType.Warning, new Regex("Several copied Mesh Prefabs match"));
            AssetDatabase.CopyAssets(new[] { OwnerPath, OwnerPrefabPath, OwnerPrefabPath }, new[] { texture, near, far });
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(SpriteImportData.TryGetMeshPrefabPath((TextureImporter)AssetImporter.GetAtPath(texture), texture, out _), Is.False);
            Assert.That(SpriteMeshAssets.IsLinkedToTexture(AssetDatabase.LoadAssetAtPath<GameObject>(near), OwnerPath), Is.True);
            Assert.That(SpriteMeshAssets.IsLinkedToTexture(AssetDatabase.LoadAssetAtPath<GameObject>(far), OwnerPath), Is.True);
            AssertLinked(OwnerPath, OwnerPrefabPath);
        }

        [UnityTest]
        public IEnumerator MultipleCopiedTextures_AreNotPairedByImportOrder()
        {
            const string first = Folder + "/First.png";
            const string second = Folder + "/Second.png";
            const string prefab = Folder + "/Copy.prefab";
            LogAssert.Expect(LogType.Warning, new Regex("Several copied textures"));
            LogAssert.Expect(LogType.Warning, new Regex("Several copied textures"));
            AssetDatabase.CopyAssets(new[] { OwnerPath, OwnerPath, OwnerPrefabPath }, new[] { first, second, prefab });
            for (int frame = 0; frame < 10; frame++) yield return null;
            foreach (string texture in new[] { first, second })
                Assert.That(SpriteImportData.TryGetMeshPrefabPath((TextureImporter)AssetImporter.GetAtPath(texture), texture, out _), Is.False);
            Assert.That(SpriteMeshAssets.IsLinkedToTexture(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), OwnerPath), Is.True);
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
