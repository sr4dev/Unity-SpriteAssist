using UnityEditor;
using UnityEngine;

namespace SpriteAssist.Tests
{
    internal static class LegacyMeshPrefabTestUtil
    {
        // 新構造の fixture が参照する Mesh を prefab に複製し、v1.4.x 以前の構造を再現する。
        public static void ConvertToLegacy(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            foreach (MeshFilter meshFilter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh sourceMesh = meshFilter.sharedMesh;
                if (sourceMesh == null) continue;

                Mesh embeddedMesh = Object.Instantiate(sourceMesh);
                embeddedMesh.name = sourceMesh.name;
                AssetDatabase.AddObjectToAsset(embeddedMesh, prefab);
                meshFilter.sharedMesh = embeddedMesh;
                EditorUtility.SetDirty(meshFilter);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
