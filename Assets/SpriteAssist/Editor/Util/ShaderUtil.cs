using UnityEngine;

namespace SpriteAssist
{
    public static class ShaderUtil
    {
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
            return FindShader(name, SpriteAssistSettings.Settings.defaultOpaqueShaderName);
        }

        public static Shader FindTransparentShader(string name)
        {
            return FindShader(name, SpriteAssistSettings.Settings.defaultTransparentShaderName);
        }
    }
}
