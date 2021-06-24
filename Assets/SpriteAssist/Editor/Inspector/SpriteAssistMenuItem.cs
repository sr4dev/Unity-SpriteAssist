using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class SpriteAssistMenuItem
    {
        [MenuItem("Assets/SpriteAssist/Swap Sprite Renderer to Mesh Prefab", priority = 700)]
        private static void SwapInProject()
        {
            Object obj = Selection.activeObject;
            string s = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);

            RendererUtil.SwapAllRecursively(s);
            EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
        }

        [MenuItem("GameObject/SpriteAssist/Swap Sprite Renderer to Mesh Prefab", priority = 21)]
        private static void SwapInHierarchySelected()
        {
            RendererUtil.SwapRendererSpriteToMeshInHierarchy(Selection.objects);
            EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
        }
    }
}