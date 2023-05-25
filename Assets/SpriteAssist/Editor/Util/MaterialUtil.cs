using UnityEngine;

namespace SpriteAssist
{
    public static class MaterialUtil
    {
        private static readonly int _mainTexId = Shader.PropertyToID("_MainTex");

        public static Texture GetMainTexture(this Material material)
        {
            //This is a workaround for the following error:
            //Material 'Transparent' with Shader 'Unlit/NewUnlitShader' doesn't have a texture property '_MainTex'
            if (material.HasProperty(_mainTexId))
            {
                return material.GetTexture(_mainTexId);
            }

            return null;
        }

        public static void SetMainTexture(this Material material, Texture texture)
        {
            //This is a workaround for the following error:
            //Material 'Transparent' with Shader 'Unlit/NewUnlitShader' doesn't have a texture property '_MainTex'
            if (material.HasProperty(_mainTexId))
            {
                material.SetTexture(_mainTexId, texture);
            }
        }
    }
}