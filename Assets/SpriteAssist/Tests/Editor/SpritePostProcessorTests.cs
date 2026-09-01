using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpriteAssist.Tests
{
    public class SpritePostProcessorTests
    {
        private const string TempRoot = "Assets/SpriteAssistPostProcessorTestTemp";
        private const string SourceTexturePath = "Assets/Example/Sprite/rebox-green-tri.png";
        private const string SourcePrefabPath = "Assets/Example/Sprite/cloud.prefab";
        private const string SingleTexturePath = TempRoot + "/RenamedSprite.png";
        private const string LegacyPrefabPath = TempRoot + "/LegacyPrefab.prefab";
        private const string RenamedPrefabPath = TempRoot + "/RenamedSprite.prefab";
        private const string MultipleTexturePath = TempRoot + "/Multiple.png";
        private const string MultiplePrefabPath = TempRoot + "/UnchangedPrefab.prefab";

        [UnityTest, Timeout(300000)]
        public IEnumerator ParallelImport_UpdatesLegacySingleSpriteButSkipsMultipleSprite()
        {
            bool renameAutomatically = SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically;
            SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically = true;

            try
            {
                PrepareFixtures(out Vector3[] multipleVertices, out int[] multipleTriangles);
                Touch(SingleTexturePath);
                Touch(MultipleTexturePath);
                AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);

                yield return null;
                yield return null;

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SingleTexturePath);
                GameObject renamedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RenamedPrefabPath);
                Mesh renamedMesh = renamedPrefab.GetComponent<MeshFilter>().sharedMesh;

                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPrefabPath), Is.Null);
                Assert.That(renamedMesh.vertices, Is.EqualTo(sprite.vertices.ToVector3()));
                Assert.That(renamedMesh.triangles, Is.EqualTo(sprite.triangles.ToInt()));

                GameObject multiplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplePrefabPath);
                Mesh multipleMesh = multiplePrefab.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(TempRoot + "/Multiple.prefab"), Is.Null);
                Assert.That(multipleMesh.vertices, Is.EqualTo(multipleVertices));
                Assert.That(multipleMesh.triangles, Is.EqualTo(multipleTriangles));
            }
            finally
            {
                SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically = renameAutomatically;
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        private static void PrepareFixtures(out Vector3[] multipleVertices, out int[] multipleTriangles)
        {
            AssetDatabase.DeleteAsset(TempRoot);
            Assert.That(AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length)), Is.Not.Empty);
            Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, SingleTexturePath), Is.True);
            Assert.That(AssetDatabase.CopyAsset(SourcePrefabPath, LegacyPrefabPath), Is.True);
            Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, MultipleTexturePath), Is.True);
            Assert.That(AssetDatabase.CopyAsset(SourcePrefabPath, MultiplePrefabPath), Is.True);

            TextureImporter singleImporter = AssetImporter.GetAtPath(SingleTexturePath) as TextureImporter;
            GameObject legacyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPrefabPath);
            var legacyIdentifier = new AssetImporter.SourceAssetIdentifier(
                typeof(GameObject), Path.GetFileNameWithoutExtension(SingleTexturePath));
            singleImporter!.AddRemap(legacyIdentifier, legacyPrefab);
            Assert.That(AssetDatabase.WriteImportSettingsIfDirty(SingleTexturePath), Is.True);

            TextureImporter multipleImporter = AssetImporter.GetAtPath(MultipleTexturePath) as TextureImporter;
            GameObject multiplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MultiplePrefabPath);
            multipleImporter!.spriteImportMode = SpriteImportMode.Multiple;
            var identifier = new AssetImporter.SourceAssetIdentifier(
                typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER);
            multipleImporter.AddRemap(identifier, multiplePrefab);
            Assert.That(AssetDatabase.WriteImportSettingsIfDirty(MultipleTexturePath), Is.True);

            Mesh multipleMesh = multiplePrefab.GetComponent<MeshFilter>().sharedMesh;
            multipleVertices = multipleMesh.vertices;
            multipleTriangles = multipleMesh.triangles;
        }

        private static void Touch(string assetPath)
        {
            using FileStream stream = new FileStream(assetPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            stream.WriteByte(0);
        }
    }
}
