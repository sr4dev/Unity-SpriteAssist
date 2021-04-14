using UnityEngine;

namespace SpriteAssist
{
    public static class ShaderUtil
    {
        public const string RENDER_SHADER_TRANSPARENT = "Unlit/Transparent";
        public const string RENDER_SHADER_OPAQUE = "Unlit/Texture";

        public static Shader FindShader(string name, string fallback)
        {
            Shader shader = null;

            if (string.IsNullOrEmpty(name) == false)
            {
                shader = Shader.Find(name);
            }

            if (shader == null)
            {
                shader = Shader.Find(fallback);
            }

            return shader;
        }

        public static Shader FindOpaqueShader(string name)
        {
            return FindShader(name, RENDER_SHADER_OPAQUE);
        }

        public static Shader FindTransparentShader(string name)
        {
            return FindShader(name, RENDER_SHADER_TRANSPARENT);
        }
    }
}