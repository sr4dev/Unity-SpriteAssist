using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class SpriteAssistMenuItem
    {
        [MenuItem("Assets/SpriteAssist/Replace Texture", priority = 0, validate = true)]
        private static bool ReplaceTextureValidate()
        {
            return ResizeUtil.ReplaceTextureValidate();
        }

        [MenuItem("Assets/SpriteAssist/Replace Texture", priority = 0)]
        private static void ReplaceTexture()
        {
            ResizeUtil.ReplaceTexture();
        }

        [MenuItem("Assets/SpriteAssist/Swap Sprite Renderer to Mesh Prefab", priority = 700)]
        private static void SwapInProject()
        {
            Object obj = Selection.activeObject;
            string s = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);

            RendererUtil.SwapAllRecursively(s);
            EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
        }

        [MenuItem("Assets/SpriteAssist/Scale Textures to Multiple of 4", false, 2000)]
        private static void ScaleTextureToMultipleOf4()
        {
            ResizeUtil.ResizeSelected(ResizeUtil.ResizeMethod.Scale);
        }

        [MenuItem("Assets/SpriteAssist/Add Alpha to Textures as Multiple of 4", false, 2000)]
        private static void AddAlphaTextureToMultipleOf4()
        {
            ResizeUtil.ResizeSelected(ResizeUtil.ResizeMethod.AddAlphaOrCropArea);
        }

        [MenuItem("GameObject/SpriteAssist/Swap Sprite Renderer to Mesh Prefab", priority = 21)]
        private static void SwapInHierarchySelected()
        {
            RendererUtil.SwapRendererSpriteToMeshInHierarchy(Selection.objects);
            EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
        }
    }
}