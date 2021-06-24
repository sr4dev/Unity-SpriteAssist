using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpriteAssistSettings : ScriptableObject
    {
        private const string SETTINGS_DIRECTORY = "Assets/Editor/";
        private const string SETTINGS_PATH = SETTINGS_DIRECTORY + nameof(SpriteAssistSettings) + ".asset";

        private const string RENDER_SHADER_TRANSPARENT = "Unlit/Transparent";
        private const string RENDER_SHADER_OPAQUE = "Unlit/Texture";
        public const string DEFAULT_TAG = "Untagged";
        private const int THUMBNAIL_COUNT = 10;

        public string prefabNamePrefix;
        public string prefabNameSuffix;
        public string prefabRelativePath;

        public string defaultTransparentShaderName;
        public string defaultOpaqueShaderName;
        public int defaultThickness;

        public string defaultTag = DEFAULT_TAG;
        public int defaultLayer;
        public int defaultSortingLayerId;
        public int defaultSortingOrder;

        public int maxThumbnailPreviewCount;
        
        public bool enableRenameMeshPrefabAutomatically;

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
                settings.prefabNamePrefix = null;
                settings.prefabNameSuffix = null;
                settings.prefabRelativePath = null;
                settings.defaultTransparentShaderName = RENDER_SHADER_TRANSPARENT;
                settings.defaultOpaqueShaderName = RENDER_SHADER_OPAQUE;
                settings.defaultTag = DEFAULT_TAG;
                settings.maxThumbnailPreviewCount = THUMBNAIL_COUNT;
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