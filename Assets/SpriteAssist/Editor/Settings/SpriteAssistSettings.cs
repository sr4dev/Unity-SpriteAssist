using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpriteAssistSettings : ScriptableObject
    {
        public const string SETTINGS_DIRECTORY = "Assets/Editor/";
        public const string SETTINGS_PATH = SETTINGS_DIRECTORY + nameof(SpriteAssistSettings) + ".asset";

        public const string RENDER_SHADER_TRANSPARENT = "Unlit/Transparent";
        public const string RENDER_SHADER_OPAQUE = "Unlit/Texture";

        public string defaultTransparentShaderName;
        public string defaultOpaqueShaderName;

        private static SpriteAssistSettings _setting;

        public static SpriteAssistSettings Settings
        {
            get
            {
                if (_setting == null)
                {
                    _setting = GetOrCreateSettings();
                }

                return _setting;
            }
        }

        private static SpriteAssistSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SpriteAssistSettings>(SETTINGS_PATH);
            if (settings == null)
            {
                if (Directory.Exists(SETTINGS_DIRECTORY) == false)
                {
                    Directory.CreateDirectory(SETTINGS_DIRECTORY);
                }

                settings = CreateInstance<SpriteAssistSettings>();
                settings.defaultTransparentShaderName = RENDER_SHADER_TRANSPARENT;
                settings.defaultOpaqueShaderName = RENDER_SHADER_OPAQUE;
                AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}