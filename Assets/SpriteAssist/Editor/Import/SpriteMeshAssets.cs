using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    // Mesh Prefab 用の Mesh はテクスチャ import の成果物（テクスチャのサブアセット）として出力する。
    // prefab 側は MeshFilter でその Mesh を参照するだけなので、import 中に prefab を書き換える必要がない。
    public static class SpriteMeshAssets
    {
        // identifier は fileID を決めるので mode が変わっても変えない（prefab の参照を維持するため）
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

            // Complex: root は Transparent、sub は Opaque
            Mesh root = meshes.Find(m => m.name == MeshCreatorBase.RENDER_TYPE_TRANSPARENT) ?? meshes[0];
            rootMesh = root;
            subMesh = meshes.Find(m => m != root);
            return true;
        }

        // 旧構造: Mesh が prefab 自身のサブアセットとして埋め込まれている
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
