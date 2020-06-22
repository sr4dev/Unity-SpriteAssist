using System.IO;
using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    public abstract class MeshPrefabCreator
    {
        public static MeshPrefabCreator GetInstnace(SpriteConfigData.Mode mode)
        {
            switch (mode)
            {
                case SpriteConfigData.Mode.TransparentMesh:
                    return new TransparentMeshPrefabCreator();

                case SpriteConfigData.Mode.OpaqueMesh:
                    return new OpaqueMeshPrefabCreator();

                case SpriteConfigData.Mode.Complex:
                    return new ComplexMeshPrefabCreator();

                default:
                    return null;
            }
        }

        protected GameObject CreateAndSavePrefab(Sprite sprite, bool hasSubObject)
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


        protected void AddComponentsAssets(Sprite sprite, SpriteConfigData data, GameObject prefab, string renderType, string shaderName, bool isOpaque, bool isComplex)
        {
            //add components
            MeshFilter meshFilter = prefab.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = prefab.AddComponent<MeshRenderer>();

            //create new mesh
            Mesh mesh = new Mesh()
            {
                name = renderType,
            };
            SpriteUtil.UpdateMesh(sprite, data, ref mesh, isOpaque, isComplex);
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