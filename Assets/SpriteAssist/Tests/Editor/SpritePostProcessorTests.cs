using System;
using System.Collections;
using System.IO;
using System.Linq;
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
        private const int MaxWaitFrames = 30;

        [UnityTest, Timeout(300000)]
        public IEnumerator ParallelImport_GeneratesMeshWithoutWritingPrefab_AndRenamesPrefab()
        {
            bool renameAutomatically = SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically;
            SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically = true;

            try
            {
                PrepareFixtures();
                long multiplePrefabWriteTime = File.GetLastWriteTimeUtc(MultiplePrefabPath).Ticks;

                Touch(SingleTexturePath);
                Touch(MultipleTexturePath);
                AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);

                // rename は delayCall で実行され、AssetDatabase の状態次第で次フレームに再試行されることがある
                yield return WaitUntil(() => AssetDatabase.LoadAssetAtPath<GameObject>(RenamedPrefabPath) != null, "prefab rename");

                // Single: サブアセット Mesh が生成され、prefab は rename されるがファイル内容は書き換えられない
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SingleTexturePath);
                Assert.That(SpriteMeshAssets.TryGetMeshes(SingleTexturePath, out Mesh mesh, out _), Is.True);
                Assert.That(mesh.vertices, Is.EqualTo(sprite.vertices.ToVector3()));
                Assert.That(mesh.triangles, Is.EqualTo(sprite.triangles.ToInt()));

                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPrefabPath), Is.Null);
                GameObject renamedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RenamedPrefabPath);
                Assert.That(renamedPrefab, Is.Not.Null);
                // RenameAsset は root GameObject 名を書き換えるため write time は比較しない。内容が legacy のままなら import は触っていない。
                Assert.That(SpriteMeshAssets.IsLegacyMeshPrefab(renamedPrefab), Is.True, "legacy prefab is migrated only by explicit user action");

                // Multiple: 対象外。Mesh も生成されず prefab も触られない
                Assert.That(SpriteMeshAssets.TryGetMeshes(MultipleTexturePath, out _, out _), Is.False);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(TempRoot + "/Multiple.prefab"), Is.Null);
                Assert.That(File.GetLastWriteTimeUtc(MultiplePrefabPath).Ticks, Is.EqualTo(multiplePrefabWriteTime));

                // 移行後の prefab はサブアセット Mesh を参照し、以降の import で追従する
                Assert.That(MeshPrefabMigration.Migrate(SingleTexturePath), Is.True);
                GameObject migratedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RenamedPrefabPath);
                Assert.That(SpriteMeshAssets.IsLinkedToTexture(migratedPrefab, SingleTexturePath), Is.True);
                Assert.That(AssetDatabase.LoadAllAssetsAtPath(RenamedPrefabPath).OfType<Mesh>(), Is.Empty);

                long migratedPrefabWriteTime = File.GetLastWriteTimeUtc(RenamedPrefabPath).Ticks;
                Touch(SingleTexturePath);
                AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);

                yield return null;
                yield return null;

                migratedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RenamedPrefabPath);
                Mesh referencedMesh = migratedPrefab.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(referencedMesh, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(referencedMesh), Is.EqualTo(SingleTexturePath));
                Assert.That(File.GetLastWriteTimeUtc(RenamedPrefabPath).Ticks, Is.EqualTo(migratedPrefabWriteTime));
            }
            finally
            {
                SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically = renameAutomatically;
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string description)
        {
            for (int i = 0; i < MaxWaitFrames; i++)
            {
                if (condition()) yield break;
                yield return null;
            }

            Assert.That(condition(), Is.True, $"Timed out waiting for {description}");
        }

        private static void PrepareFixtures()
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
        }

        private static void Touch(string assetPath)
        {
            using FileStream stream = new FileStream(assetPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            stream.WriteByte(0);
        }
    }
}
