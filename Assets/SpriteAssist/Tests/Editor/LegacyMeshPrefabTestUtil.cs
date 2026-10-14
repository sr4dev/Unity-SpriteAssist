using UnityEditor;
using UnityEngine;

namespace SpriteAssist.Tests
{
    internal static class LegacyMeshPrefabTestUtil
    {
        // Copy the Mesh referenced by the new-format fixture into the prefab to reproduce the format used through v1.4.x.
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
