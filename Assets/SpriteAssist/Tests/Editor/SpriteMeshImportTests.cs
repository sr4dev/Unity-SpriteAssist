using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist.Tests
{
    public class SpriteMeshImportTests
    {
        private const string TempRoot = "Assets/SpriteAssistMeshImportTestTemp";
        private const string SourceTexturePath = "Assets/Example/Sprite/rebox-green-tri.png";
        private const string SourcePrefabPath = "Assets/Example/Sprite/cloud.prefab";
        private const string UnlinkedTexturePath = TempRoot + "/Unlinked.png";

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
        public void Import_GeneratesTextureSubAssetMesh_AndMigrationLinksPrefab()
        {
            PrepareFixtures();

            try
            {
                foreach (SpriteConfigData.Mode mode in SingleMeshModes)
                {
                    AssertSingleModeImport(mode);
                }

                AssertComplexModeImport();
                AssertUnlinkedTextureHasNoMesh();
                AssertMeshFileIdIsStableAcrossModeChange();
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        [Test, Timeout(300000)]
        public void Migrate_OrphanLegacyPrefab_IsSkipped()
        {
            const string OrphanTexturePath = TempRoot + "/Orphan.png";
            const string OrphanPrefabPath = TempRoot + "/Orphan.prefab";

            AssetDatabase.DeleteAsset(TempRoot);
            Assert.That(AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length)), Is.Not.Empty);

            try
            {
                // cloud.png(ComplexMesh 設定) の複製と、Material だけがその複製を指す未リンク legacy prefab を用意する
                Assert.That(AssetDatabase.CopyAsset("Assets/Example/Sprite/cloud.png", OrphanTexturePath), Is.True);
                Assert.That(AssetDatabase.CopyAsset(SourcePrefabPath, OrphanPrefabPath), Is.True);

                TextureImporter importer = AssetImporter.GetAtPath(OrphanTexturePath) as TextureImporter;
                foreach (AssetImporter.SourceAssetIdentifier identifier in importer!.GetExternalObjectMap().Keys.ToArray())
                {
                    importer.RemoveRemap(identifier);
                }
                importer.SaveAndReimport();

                LegacyMeshPrefabTestUtil.ConvertToLegacy(OrphanPrefabPath);

                Texture2D orphanTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OrphanTexturePath);
                foreach (Material material in AssetDatabase.LoadAllAssetsAtPath(OrphanPrefabPath).OfType<Material>())
                {
                    material.SetMainTexture(orphanTexture);
                    EditorUtility.SetDirty(material);
                }
                AssetDatabase.SaveAssets();

                Assert.That(SpriteImportData.HasMeshPrefabLink(importer, OrphanTexturePath), Is.False);
                Assert.That(SpriteMeshAssets.TryGetMeshes(OrphanTexturePath, out _, out _), Is.False);
                Assert.That(SpriteMeshAssets.IsLegacyMeshPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(OrphanPrefabPath)), Is.True);

                MeshPrefabMigration.Result result = MeshPrefabMigration.Migrate(new[] { OrphanPrefabPath });
                Assert.That(result.linked, Is.Zero);
                Assert.That(result.migrated, Is.Zero);
                Assert.That(result.skipped, Is.EqualTo(1));

                importer = AssetImporter.GetAtPath(OrphanTexturePath) as TextureImporter;
                Assert.That(SpriteImportData.TryGetMeshPrefabPath(importer, OrphanTexturePath, out _), Is.False);
                Assert.That(SpriteMeshAssets.TryGetMeshes(OrphanTexturePath, out _, out _), Is.False);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OrphanPrefabPath);
                Assert.That(SpriteMeshAssets.IsLegacyMeshPrefab(prefab), Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        [Test, Timeout(300000)]
        public void Migrate_RegularMeshPrefab_IsNotTreatedAsLegacy()
        {
            const string TexturePath = TempRoot + "/Regular.png";
            const string MeshPath = TempRoot + "/Regular.asset";
            const string MaterialPath = TempRoot + "/Regular.mat";
            const string PrefabPath = TempRoot + "/Regular.prefab";

            AssetDatabase.DeleteAsset(TempRoot);
            Assert.That(AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length)), Is.Not.Empty);

            try
            {
                Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, TexturePath), Is.True);

                var mesh = new Mesh { name = "Regular" };
                mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
                mesh.triangles = new[] { 0, 1, 2 };
                AssetDatabase.CreateAsset(mesh, MeshPath);

                var material = new Material(Shader.Find("Unlit/Texture"));
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                AssetDatabase.CreateAsset(material, MaterialPath);

                var instance = new GameObject("Regular");
                instance.AddComponent<MeshFilter>().sharedMesh = mesh;
                instance.AddComponent<MeshRenderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                Object.DestroyImmediate(instance);

                MeshPrefabMigration.Result result = MeshPrefabMigration.Migrate(new[] { PrefabPath });
                Assert.That(result.migrated, Is.Zero);
                Assert.That(result.linked, Is.Zero);
                Assert.That(result.skipped, Is.Zero);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(mesh));
                Assert.That(prefab.GetComponent<MeshRenderer>().sharedMaterial, Is.EqualTo(material));
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        // migration / reimport 更新時に prefab 側で変更した Layer / Tag / Sorting / Material(shader) が保持されること
        [Test, Timeout(300000)]
        public void Migrate_PreservesPrefabRendererSettingsAndMaterial()
        {
            const string TexturePath = TempRoot + "/Preserve.png";
            const string PrefabPath = TempRoot + "/Preserve.prefab";
            const int CustomLayer = 5;
            const int CustomSortingOrder = 42;
            const string CustomShaderName = "Unlit/Color";
            Color customColor = new Color(0.1f, 0.2f, 0.3f, 1f);

            AssetDatabase.DeleteAsset(TempRoot);
            Assert.That(AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length)), Is.Not.Empty);

            try
            {
                Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, TexturePath), Is.True);
                Assert.That(AssetDatabase.CopyAsset(SourcePrefabPath, PrefabPath), Is.True);
                LegacyMeshPrefabTestUtil.ConvertToLegacy(PrefabPath);

                TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                var configData = new SpriteConfigData
                {
                    mode = SpriteConfigData.Mode.TransparentMesh,
                    transparentShaderName = "Unlit/Transparent",
                    opaqueShaderName = "Unlit/Texture",
                    overrideLayer = true,
                    layer = 0,
                    overrideSortingLayer = true,
                    sortingOrder = 0
                };
                importer!.userData = JsonUtility.ToJson(configData);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER), prefab);
                Assert.That(AssetDatabase.WriteImportSettingsIfDirty(TexturePath), Is.True);
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

                // 利用者が prefab 側で変更した状態を再現する
                Shader customShader = Shader.Find(CustomShaderName);
                Assert.That(customShader, Is.Not.Null);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                prefab.layer = CustomLayer;
                MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
                renderer.sortingOrder = CustomSortingOrder;
                Material customMaterial = renderer.sharedMaterial;
                customMaterial.shader = customShader;
                customMaterial.color = customColor;
                EditorUtility.SetDirty(customMaterial);
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();

                Assert.That(SpriteMeshAssets.IsLegacyMeshPrefab(prefab), Is.True);

                // migration
                Assert.That(MeshPrefabMigration.Migrate(TexturePath), Is.True);
                AssertPreserved(PrefabPath, TexturePath, CustomLayer, CustomSortingOrder, customShader, customColor, "migration");

                // reimport 後の更新（Apply 相当）
                Assert.That(MeshPrefabMigration.Migrate(TexturePath), Is.True);
                AssertPreserved(PrefabPath, TexturePath, CustomLayer, CustomSortingOrder, customShader, customColor, "update");
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        private static void AssertPreserved(string prefabPath, string texturePath, int layer, int sortingOrder, Shader shader, Color color, string label)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();

            Assert.That(SpriteMeshAssets.IsLegacyMeshPrefab(prefab), Is.False, label);
            Assert.That(SpriteMeshAssets.IsLinkedToTexture(prefab, texturePath), Is.True, label);
            Assert.That(prefab.layer, Is.EqualTo(layer), label);
            Assert.That(renderer.sortingOrder, Is.EqualTo(sortingOrder), label);
            Assert.That(renderer.sharedMaterial, Is.Not.Null, label);
            Assert.That(renderer.sharedMaterial.shader, Is.EqualTo(shader), label);
            Assert.That(renderer.sharedMaterial.color, Is.EqualTo(color), label);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<Mesh>(), Is.Empty, label);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<Material>().Count(), Is.EqualTo(1), label);
        }

        // 新規作成時は Sprite 設定の Layer / Sorting が初期値として適用されること（overrideLayer の判定修正を含む）
        [Test, Timeout(300000)]
        public void Create_AppliesInitialLayerAndSortingFromConfig()
        {
            const string TexturePath = TempRoot + "/Create.png";
            const int ConfigLayer = 5;
            const int ConfigSortingOrder = 7;

            AssetDatabase.DeleteAsset(TempRoot);
            Assert.That(AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length)), Is.Not.Empty);

            try
            {
                Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, TexturePath), Is.True);

                TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                var configData = new SpriteConfigData
                {
                    mode = SpriteConfigData.Mode.TransparentMesh,
                    transparentShaderName = "Unlit/Transparent",
                    opaqueShaderName = "Unlit/Texture",
                    overrideLayer = true,
                    layer = ConfigLayer,
                    overrideSortingLayer = true,
                    sortingOrder = ConfigSortingOrder
                };
                importer!.userData = JsonUtility.ToJson(configData);
                Assert.That(AssetDatabase.WriteImportSettingsIfDirty(TexturePath), Is.True);
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
                importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                MeshCreatorBase meshCreator = MeshCreatorBase.GetInstance(configData.mode);

                // Inspector の Apply(Create) と同じ流れ: リンク → reimport(サブアセット Mesh 生成) → prefab 更新
                using (SpriteImportData importData = new SpriteImportData(sprite, importer, TexturePath, createDummySprite: false))
                {
                    MeshPrefabService.SetMeshPrefabContainer(importData, meshCreator, configData, removeOldMeshPrefab: false, attachedMeshPrefab: null);
                    EditorUtility.SetDirty(importer);
                    AssetDatabase.WriteImportSettingsIfDirty(TexturePath);
                    importer.SaveAndReimport();

                    MeshPrefabService.UpdateSubAssetsInMeshPrefab(importData, meshCreator, configData);
                    AssetDatabase.WriteImportSettingsIfDirty(TexturePath);
                }

                AssetDatabase.SaveAssets();

                importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                Assert.That(SpriteImportData.TryGetMeshPrefabPath(importer, TexturePath, out string prefabPath), Is.True);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();

                Assert.That(prefab.layer, Is.EqualTo(ConfigLayer));
                Assert.That(renderer.sortingOrder, Is.EqualTo(ConfigSortingOrder));
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Unlit/Transparent"));
                Assert.That(SpriteMeshAssets.IsLinkedToTexture(prefab, TexturePath), Is.True);
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

            foreach (SpriteConfigData.Mode mode in SingleMeshModes.Append(SpriteConfigData.Mode.ComplexMesh))
            {
                string texturePath = GetTexturePath(mode);
                string prefabPath = GetPrefabPath(mode);
                Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, texturePath), Is.True);
                Assert.That(AssetDatabase.CopyAsset(SourcePrefabPath, prefabPath), Is.True);
                LegacyMeshPrefabTestUtil.ConvertToLegacy(prefabPath);

                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
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

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER), prefab);
                Assert.That(AssetDatabase.WriteImportSettingsIfDirty(texturePath), Is.True);
            }

            Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, UnlinkedTexturePath), Is.True);
            TextureImporter unlinkedImporter = AssetImporter.GetAtPath(UnlinkedTexturePath) as TextureImporter;
            unlinkedImporter!.userData = JsonUtility.ToJson(new SpriteConfigData { mode = SpriteConfigData.Mode.TransparentMesh });
            Assert.That(AssetDatabase.WriteImportSettingsIfDirty(UnlinkedTexturePath), Is.True);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
        }

        private static void AssertSingleModeImport(SpriteConfigData.Mode mode)
        {
            string texturePath = GetTexturePath(mode);
            string prefabPath = GetPrefabPath(mode);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            SpriteConfigData configData = SpriteConfigData.GetData(importer!.userData);

            Assert.That(SpriteMeshAssets.TryGetMeshes(texturePath, out Mesh rootMesh, out Mesh subMesh), Is.True, mode.ToString());
            Assert.That(subMesh, Is.Null, mode.ToString());
            Assert.That(AssetDatabase.GetAssetPath(rootMesh), Is.EqualTo(texturePath), mode.ToString());

            // import 出力は OverrideGeometry 済みスプライトのジオメトリ（+thickness）と一致する
            Vector3[] expectedVertices = sprite.vertices.ToVector3();
            int[] expectedTriangles = sprite.triangles.ToInt();
            if (mode != SpriteConfigData.Mode.OpaqueEdgeGridMesh && configData.thickness > 0)
            {
                TriangulationUtil.ExpandMeshThickness(ref expectedVertices, ref expectedTriangles, configData.thickness);
            }

            Assert.That(rootMesh.vertices, Is.EqualTo(expectedVertices), mode.ToString());
            Assert.That(rootMesh.triangles, Is.EqualTo(expectedTriangles), mode.ToString());

            // import 前の legacy prefab は import では書き換えられない
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(SpriteMeshAssets.IsLegacyMeshPrefab(prefab), Is.True, mode.ToString());

            // 移行後は prefab がテクスチャのサブアセット Mesh を参照し、埋め込み Mesh は消える
            Assert.That(MeshPrefabMigration.Migrate(texturePath), Is.True, mode.ToString());
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(SpriteMeshAssets.IsLegacyMeshPrefab(prefab), Is.False, mode.ToString());
            Assert.That(SpriteMeshAssets.IsLinkedToTexture(prefab, texturePath), Is.True, mode.ToString());
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(rootMesh), mode.ToString());
            Assert.That(prefab.transform.childCount, Is.Zero, mode.ToString());
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<Mesh>(), Is.Empty, mode.ToString());
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<Material>().Count(), Is.EqualTo(1), mode.ToString());
        }

        private static void AssertComplexModeImport()
        {
            const SpriteConfigData.Mode mode = SpriteConfigData.Mode.ComplexMesh;
            string texturePath = GetTexturePath(mode);
            string prefabPath = GetPrefabPath(mode);

            Assert.That(SpriteMeshAssets.TryGetMeshes(texturePath, out Mesh rootMesh, out Mesh subMesh), Is.True);
            Assert.That(rootMesh, Is.Not.Null);
            Assert.That(subMesh, Is.Not.Null);
            Assert.That(rootMesh.name, Is.EqualTo(MeshCreatorBase.RENDER_TYPE_TRANSPARENT));
            Assert.That(subMesh.name, Is.EqualTo(MeshCreatorBase.RENDER_TYPE_OPAQUE));
            Assert.That(rootMesh.vertexCount, Is.GreaterThan(0));
            Assert.That(subMesh.vertexCount, Is.GreaterThan(0));

            Assert.That(MeshPrefabMigration.Migrate(texturePath), Is.True);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(rootMesh));
            Assert.That(prefab.transform.childCount, Is.EqualTo(1));
            Assert.That(prefab.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(subMesh));
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<Mesh>(), Is.Empty);
        }

        private static void AssertUnlinkedTextureHasNoMesh()
        {
            Assert.That(SpriteMeshAssets.TryGetMeshes(UnlinkedTexturePath, out _, out _), Is.False);
        }

        private static void AssertMeshFileIdIsStableAcrossModeChange()
        {
            string texturePath = GetTexturePath(SpriteConfigData.Mode.TransparentMesh);
            string prefabPath = GetPrefabPath(SpriteConfigData.Mode.TransparentMesh);

            Assert.That(SpriteMeshAssets.TryGetMeshes(texturePath, out Mesh before, out _), Is.True);
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(before, out string guidBefore, out long fileIdBefore), Is.True);

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            SpriteConfigData configData = SpriteConfigData.GetData(importer!.userData);
            configData.mode = SpriteConfigData.Mode.OpaqueMesh;
            importer.userData = JsonUtility.ToJson(configData);
            Assert.That(AssetDatabase.WriteImportSettingsIfDirty(texturePath), Is.True);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            Assert.That(SpriteMeshAssets.TryGetMeshes(texturePath, out Mesh after, out _), Is.True);
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(after, out string guidAfter, out long fileIdAfter), Is.True);
            Assert.That(guidAfter, Is.EqualTo(guidBefore));
            Assert.That(fileIdAfter, Is.EqualTo(fileIdBefore), "root mesh fileID must not change with mode");
            Assert.That(after.name, Is.EqualTo(MeshCreatorBase.RENDER_TYPE_OPAQUE));

            // prefab の参照は Apply 無しでも維持される
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(after));
        }

        private static string GetTexturePath(SpriteConfigData.Mode mode)
        {
            return $"{TempRoot}/{Enum.GetName(typeof(SpriteConfigData.Mode), mode)}.png";
        }

        private static string GetPrefabPath(SpriteConfigData.Mode mode)
        {
            return $"{TempRoot}/{Enum.GetName(typeof(SpriteConfigData.Mode), mode)}.prefab";
        }
    }
}
