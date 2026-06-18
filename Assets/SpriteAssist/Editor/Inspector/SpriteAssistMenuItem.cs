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

        [MenuItem("Assets/SpriteAssist/Resize/Replace with Resized Textures", priority = 0, validate = true)]
        private static bool ReplaceWithResizedTexturesValidate()
        {
            return ResizeUtil.ReplaceWithResizedTexturesValidate();
        }

        [MenuItem("Assets/SpriteAssist/Resize/Replace with Resized Textures", priority = 0)]
        private static void ReplaceWithResizedTextures()
        {
            ResizeUtil.ReplaceWithResizedTextures();
        }

        [MenuItem("Assets/SpriteAssist/Resize/To Multiple of 4/Scale Textures", false, 2000)]
        private static void ScaleTextureToMultipleOf4()
        {
            ResizeUtil.ResizeSelected(ResizeUtil.ResizeMethod.Scale, ResizeUtil.SizeConstraint.MultipleOf4);
        }

        [MenuItem("Assets/SpriteAssist/Resize/To Multiple of 4/Expand or Crop Textures", false, 2001)]
        private static void ExpandOrCropTextureToMultipleOf4()
        {
            ResizeUtil.ResizeSelected(ResizeUtil.ResizeMethod.ExpandOrCrop, ResizeUtil.SizeConstraint.MultipleOf4);
        }

        [MenuItem("Assets/SpriteAssist/Resize/To Power of Two/Scale Textures", false, 2010)]
        private static void ScaleTextureToPowerOfTwo()
        {
            ResizeUtil.ResizeSelected(ResizeUtil.ResizeMethod.Scale, ResizeUtil.SizeConstraint.PowerOfTwo);
        }

        [MenuItem("Assets/SpriteAssist/Resize/To Power of Two/Expand or Crop Textures", false, 2011)]
        private static void ExpandOrCropTextureToPowerOfTwo()
        {
            ResizeUtil.ResizeSelected(ResizeUtil.ResizeMethod.ExpandOrCrop, ResizeUtil.SizeConstraint.PowerOfTwo);
        }

        [MenuItem("GameObject/SpriteAssist/Swap Sprite Renderer to Mesh Prefab", priority = 21)]
        private static void SwapInHierarchySelected()
        {
            RendererUtil.SwapRendererSpriteToMeshInHierarchy(Selection.objects);
            EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
        }
    }
}
