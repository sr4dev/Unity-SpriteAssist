using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    // Output Mesh Prefab Meshes as texture import artifacts (texture subassets).
    // The prefab only references these Meshes through MeshFilters, so import need not rewrite the prefab.
    public static class SpriteMeshAssets
    {
        // Identifiers determine fileIDs; keep them stable across mode changes to preserve prefab references.
        public const string ROOT_MESH_IDENTIFIER = "SpriteAssist.Mesh";
        public const string SUB_MESH_IDENTIFIER = "SpriteAssist.Mesh.Sub";

        public static bool TryGetMeshes(string textureAssetPath, out Mesh rootMesh, out Mesh subMesh)
        {
            rootMesh = null;
            subMesh = null;

            if (string.IsNullOrEmpty(textureAssetPath)) return false;

            var meshes = new List<Mesh>(2);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(textureAssetPath))
            {
                if (asset is Mesh mesh) meshes.Add(mesh);
            }

            if (meshes.Count == 0) return false;

            if (meshes.Count == 1)
            {
                rootMesh = meshes[0];
                return true;
            }

            // Complex: root is Transparent, sub is Opaque.
            Mesh root = meshes.Find(m => m.name == MeshCreatorBase.RENDER_TYPE_TRANSPARENT) ?? meshes[0];
            rootMesh = root;
            subMesh = meshes.Find(m => m != root);
            return true;
        }

        // Legacy format: Meshes are embedded as subassets of the prefab itself.
        public static bool IsLegacyMeshPrefab(GameObject meshPrefab)
        {
            if (meshPrefab == null) return false;

            string prefabPath = AssetDatabase.GetAssetPath(meshPrefab);
            if (string.IsNullOrEmpty(prefabPath)) return false;

            foreach (MeshFilter meshFilter in meshPrefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh != null && AssetDatabase.GetAssetPath(mesh) == prefabPath)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsLinkedToTexture(GameObject meshPrefab, string textureAssetPath)
        {
            if (meshPrefab == null || string.IsNullOrEmpty(textureAssetPath)) return false;

            MeshFilter meshFilter = meshPrefab.GetComponent<MeshFilter>();
            return meshFilter != null && meshFilter.sharedMesh != null &&
                   AssetDatabase.GetAssetPath(meshFilter.sharedMesh) == textureAssetPath;
        }
    }
}
