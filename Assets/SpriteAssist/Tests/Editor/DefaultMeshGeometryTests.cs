using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist.Tests
{
    public class DefaultMeshGeometryTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void SpriteClampDoesNotChangePersistentMeshBoundaryOrRestoredUv(bool split)
        {
            var texture = new Texture2D(285, 396);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 285, 396), new Vector2(.5f, .5f), 100);
            Mesh mesh = null;
            try
            {
                var info = new TextureInfo(sprite, "");
                var geometry = new DefaultMeshGeometry(new[] { new Vector2(-1.55f,-2.205f), new Vector2(-1.55f,2.205f),
                    new Vector2(1.55f,2.205f), new Vector2(1.55f,-2.205f) }, new ushort[] { 0,1,2,0,2,3 });
                var spritePixels = geometry.GetSpritePixelVertices(info);
                var config = new SpriteConfigData { isCorrectNormal = split, meshUv = new MeshUvTransform
                    { u = new Vector3(285f/310f,0,12.5f/310f), v = new Vector3(0,396f/441f,22.5f/441f) } };
                mesh = geometry.CreateMesh(info, config);
                Assert.That(mesh.bounds.size.x, Is.EqualTo(3.10f).Within(1e-5));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(4.41f).Within(1e-5));
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    var point = mesh.vertices[i];
                    Assert.That(mesh.uv[i].x, Is.EqualTo((point.x+1.55f)/3.1f).Within(1e-6));
                    Assert.That(mesh.uv[i].y, Is.EqualTo((point.y+2.205f)/4.41f).Within(1e-6));
                }
                foreach (var pixel in spritePixels)
                {
                    var point = (pixel - info.pivot) / info.pixelPerUnit;
                    Assert.That(Mathf.Abs(point.x), Is.LessThanOrEqualTo(1.42501f));
                    Assert.That(Mathf.Abs(point.y), Is.LessThanOrEqualTo(1.98001f));
                }
            }
            finally
            {
                if (mesh) UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void MissingUvInputKeepsNormalMappingAndUnsupportedVersionFails()
        {
            Assert.That(SpriteConfigData.GetData("{\"mode\":0}").meshUv, Is.Null);
            var mapping = new MeshUvTransform();
            Assert.That(mapping.Evaluate(new Vector2(-.2f,1.2f)), Is.EqualTo(new Vector2(-.2f,1.2f)));
            mapping.version = 99;
            Assert.Throws<InvalidOperationException>(() => mapping.Evaluate(Vector2.zero));
        }

        [Test]
        public void NonFiniteUvInputFailsInsteadOfPublishingBrokenMesh()
        {
            var mapping = new MeshUvTransform { u = new Vector3(float.NaN,0,0) };
            Assert.Throws<InvalidOperationException>(() => mapping.Evaluate(Vector2.zero));
        }

        [Test, Timeout(180000)]
        public void ResizedImportPreservesSourceBoundaryUvAndParticleReferences()
        {
            const string folder = "Assets/SpriteAssistMigrationTestTemp";
            const string path = folder + "/Cloth.png";
            const string prefabPath = folder + "/Particles.prefab";
            if (System.IO.Directory.Exists(folder)) throw new InvalidOperationException("Test fixture already exists");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            GameObject instance = null;
            try
            {
                // Downscaled import fixture independent of images in the consuming project.
                var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                try
                {
                    texture.SetPixels32(System.Linq.Enumerable.Repeat(new Color32(255,255,255,255), 512*512).ToArray());
                    texture.Apply();
                    System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                foreach (var key in new System.Collections.Generic.List<AssetImporter.SourceAssetIdentifier>(importer.GetExternalObjectMap().Keys)) importer.RemoveRemap(key);
                importer.maxTextureSize = 128;
                foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                {
                    var settings = importer.GetPlatformTextureSettings(platform);
                    settings.overridden = true; settings.maxTextureSize = 128;
                    importer.SetPlatformTextureSettings(settings);
                }
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                var serialized = new SerializedObject(importer);
                var outlines = serialized.FindProperty("m_SpriteSheet.m_Outline"); outlines.arraySize = 1;
                var contour = outlines.GetArrayElementAtIndex(0); contour.arraySize = 4;
                var points = new[] { new Vector2(-155,-220.5f),new Vector2(-155,220.5f),new Vector2(155,220.5f),new Vector2(155,-220.5f) };
                for (int i=0;i<4;i++) contour.GetArrayElementAtIndex(i).vector2Value = points[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                importer.userData = JsonUtility.ToJson(new SpriteConfigData { meshUv = new MeshUvTransform
                    { u = new Vector3(width/310f,0,.5f-width/620f), v = new Vector3(0,height/441f,.5f-height/882f) } });
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite.pixelsPerUnit, Is.LessThan(100));
                Assert.That(SpriteMeshAssets.TryGetMeshes(path, out var first, out _), Is.True);
                Assert.That(first.bounds.size.x, Is.EqualTo(3.1f).Within(.0001));
                Assert.That(first.bounds.size.y, Is.EqualTo(4.41f).Within(.0001));
                for (int i = 0; i < first.vertexCount; i++)
                {
                    Assert.That(first.uv[i].x, Is.EqualTo((first.vertices[i].x+1.55f)/3.1f).Within(.00001));
                    Assert.That(first.uv[i].y, Is.EqualTo((first.vertices[i].y+2.205f)/4.41f).Within(.00001));
                }
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(first, out string guid, out long id);
                instance = new GameObject("Particles");
                var particles = instance.AddComponent<ParticleSystem>();
                var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Mesh; shape.mesh = first;
                var renderer = instance.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = first;
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                UnityEngine.Object.DestroyImmediate(instance); instance = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.DontDownloadFromCacheServer);
                Assert.That(SpriteMeshAssets.TryGetMeshes(path, out var second, out _), Is.True);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(second, out string newGuid, out long newId);
                Assert.That(newGuid, Is.EqualTo(guid)); Assert.That(newId, Is.EqualTo(id));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab.GetComponent<ParticleSystem>().shape.mesh, Is.EqualTo(second));
                Assert.That(prefab.GetComponent<ParticleSystemRenderer>().mesh, Is.EqualTo(second));
            }
            finally
            {
                if (instance) UnityEngine.Object.DestroyImmediate(instance);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
