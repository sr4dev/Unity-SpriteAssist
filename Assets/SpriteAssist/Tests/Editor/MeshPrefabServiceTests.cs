using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist.Tests
{
    public class MeshPrefabServiceTests
    {
        private const string TempRoot = "Assets/SpriteAssistGeometryTestTemp";
        private const string SourceTexturePath = "Assets/Example/Sprite/rebox-green-tri.png";

        private static readonly SpriteConfigData.Mode[] SingleMeshModes =
        {
            SpriteConfigData.Mode.UnityDefaultForTransparent,
            SpriteConfigData.Mode.UnityDefaultForOpaque,
            SpriteConfigData.Mode.TransparentMesh,
            SpriteConfigData.Mode.OpaqueMesh,
            SpriteConfigData.Mode.GridMesh,
            SpriteConfigData.Mode.OpaqueEdgeGridMesh,
            SpriteConfigData.Mode.PixelMesh
        };

        [Test, Timeout(300000)]
        public void ImportedSpriteGeometry_MatchesLegacyPrefabGeometryForSingleMeshModes()
        {
            PrepareFixtures();

            try
            {
                foreach (SpriteConfigData.Mode mode in SingleMeshModes)
                {
                    AssertGeometryMatches(mode);
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        private static void PrepareFixtures()
        {
            AssetDatabase.DeleteAsset(TempRoot);
            Assert.That(AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length)), Is.Not.Empty);

            foreach (SpriteConfigData.Mode mode in SingleMeshModes)
            {
                string assetPath = GetTexturePath(mode);
                Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, assetPath), Is.True);

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null);

                var configData = new SpriteConfigData
                {
                    mode = mode,
                    gridSize = 8,
                    gridTolerance = 0.5f,
                    thickness = 0.25f,
                    transparentShaderName = "Unlit/Transparent",
                    opaqueShaderName = "Unlit/Texture"
                };
                importer.userData = JsonUtility.ToJson(configData);
                Assert.That(AssetDatabase.WriteImportSettingsIfDirty(assetPath), Is.True);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
        }

        private static void AssertGeometryMatches(SpriteConfigData.Mode mode)
        {
            int meshCountBefore = Resources.FindObjectsOfTypeAll<Mesh>().Length;
            string assetPath = GetTexturePath(mode);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            SpriteConfigData configData = SpriteConfigData.GetData(importer!.userData);
            TextureInfo textureInfo = new TextureInfo(sprite, assetPath);

            GameObject expectedObject = CreateMeshObject("Expected", out Mesh expectedMesh);
            GameObject actualObject = CreateMeshObject("Actual", out Mesh actualMesh);

            try
            {
                using SpriteImportData importData = new SpriteImportData(sprite, importer, assetPath);
                MeshCreatorBase meshCreator = MeshCreatorBase.GetInstance(mode);
                meshCreator.UpdateMeshInMeshPrefab(expectedObject, sprite, importData.dummySprite, textureInfo, configData);
                Assert.That(MeshPrefabService.UpdateMeshInMeshPrefabFromImportedSprite(actualObject, sprite, textureInfo, configData), Is.True, mode.ToString());

                AssertVerticesEqual(actualMesh.vertices, expectedMesh.vertices, mode);
                Assert.That(actualMesh.triangles, Is.EqualTo(expectedMesh.triangles), mode.ToString());
                AssertUvsEqual(actualMesh.uv, expectedMesh.uv, mode);
                Assert.That(MeshPrefabService.UpdateMeshInMeshPrefabFromImportedSprite(actualObject, sprite, textureInfo, configData), Is.False, mode.ToString());
                Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Length, Is.EqualTo(meshCountBefore + 2), mode.ToString());
            }
            finally
            {
                Object.DestroyImmediate(expectedMesh);
                Object.DestroyImmediate(actualMesh);
                Object.DestroyImmediate(expectedObject);
                Object.DestroyImmediate(actualObject);
            }

            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Length, Is.EqualTo(meshCountBefore), mode.ToString());
        }

        private static GameObject CreateMeshObject(string name, out Mesh mesh)
        {
            var gameObject = new GameObject(name);
            mesh = new Mesh();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            return gameObject;
        }

        private static void AssertVerticesEqual(Vector3[] actual, Vector3[] expected, SpriteConfigData.Mode mode)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length), mode.ToString());

            float maxDistance = 0;
            for (int i = 0; i < actual.Length; i++)
            {
                maxDistance = Mathf.Max(maxDistance, Vector3.Distance(actual[i], expected[i]));
            }

            Assert.That(maxDistance, Is.LessThanOrEqualTo(0.00001f), mode.ToString());
        }

        private static void AssertUvsEqual(Vector2[] actual, Vector2[] expected, SpriteConfigData.Mode mode)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length), mode.ToString());

            float maxDistance = 0;
            for (int i = 0; i < actual.Length; i++)
            {
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(actual[i], expected[i]));
            }

            Assert.That(maxDistance, Is.LessThanOrEqualTo(0.00001f), mode.ToString());
        }

        private static string GetTexturePath(SpriteConfigData.Mode mode)
        {
            return $"{TempRoot}/{Enum.GetName(typeof(SpriteConfigData.Mode), mode)}.png";
        }
    }
}
