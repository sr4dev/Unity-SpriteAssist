using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    public abstract class MeshCreator
    {
        public const string RENDER_TYPE_TRANSPARENT = "Transparent";
        public const string RENDER_TYPE_OPAQUE = "Opaque";

        public const string RENDER_SHADER_TRANSPARENT = "Unlit/Transparent";
        public const string RENDER_SHADER_OPAQUE = "Unlit/Texture";

        private static readonly Dictionary<SpriteConfigData.Mode, MeshCreator> m_creatorCache = new Dictionary<SpriteConfigData.Mode, MeshCreator>
        {
            { SpriteConfigData.Mode.TransparentMesh, new TransparentMeshCreator() },
            { SpriteConfigData.Mode.OpaqueMesh, new OpaqueMeshCreator() },
            { SpriteConfigData.Mode.Complex, new ComplexMeshCreator() },
        };

        public IReadOnlyDictionary<SpriteConfigData.Mode, MeshCreator> CreatorCache => m_creatorCache;

        public static MeshCreator GetInstnace(SpriteConfigData.Mode mode)
        {
            switch (mode)
            {
                case SpriteConfigData.Mode.TransparentMesh:
                    return new TransparentMeshCreator();

                case SpriteConfigData.Mode.OpaqueMesh:
                    return new OpaqueMeshCreator();

                case SpriteConfigData.Mode.Complex:
                    return new ComplexMeshCreator();

                default:
                    return new DefaultMeshCreator();
            }
        }

        public static GameObject CreateAndSavePrefab(Sprite sprite, bool hasSubObject)
        {
            string name = sprite.texture.name;
            GameObject instance = new GameObject(name);

            if (hasSubObject)
            {
                GameObject subInstance = new GameObject(name + "(sub)");
                subInstance.transform.SetParent(instance.transform);
            }

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            string currentDirectory = Path.GetDirectoryName(assetPath);
            string path = Path.Combine(currentDirectory,name + ".prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        public static void AddComponentsAssets(Sprite sprite, SpriteConfigData data, GameObject prefab, string renderType, string shaderName, MeshRenderType meshRenderType)
        {
            //add components
            MeshFilter meshFilter = prefab.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = prefab.AddComponent<MeshRenderer>();

            //create new mesh
            Mesh mesh = new Mesh()
            {
                name = renderType,
            };
            SpriteUtil.UpdateMesh(sprite, data, ref mesh, meshRenderType);
            meshFilter.mesh = mesh;

            //creat new material
            Material material = new Material(Shader.Find(shaderName))
            {
                name = renderType,
                mainTexture = sprite.texture
            };
            meshRenderer.sharedMaterial = material;

            //set assets as sub-asset
            AssetDatabase.AddObjectToAsset(material, prefab);
            AssetDatabase.AddObjectToAsset(mesh, prefab);
            AssetDatabase.SaveAssets();
        }

        public abstract void OverrideGeometry(Sprite sprite, SpriteConfigData configData);

        public abstract GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data);
    }
}