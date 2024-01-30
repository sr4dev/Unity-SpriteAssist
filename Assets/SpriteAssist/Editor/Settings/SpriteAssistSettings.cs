using UnityEditor;

namespace SpriteAssist
{
    [FilePath(SETTINGS_PATH, FilePathAttribute.Location.ProjectFolder)]
    public class SpriteAssistSettings : ScriptableSingleton<SpriteAssistSettings>
    {
        private const string SETTINGS_PATH = "Assets/Editor/SpriteAssistSettings.asset";
        private const string RENDER_SHADER_TRANSPARENT = "Unlit/Transparent";
        private const string RENDER_SHADER_OPAQUE = "Unlit/Texture";
        public const string DEFAULT_TAG = "Untagged";
        private const int THUMBNAIL_COUNT = 10;

        public string prefabNamePrefix;
        public string prefabNameSuffix;
        public string prefabRelativePath;

        public string defaultTransparentShaderName = RENDER_SHADER_TRANSPARENT;
        public string defaultOpaqueShaderName = RENDER_SHADER_OPAQUE;
        //public int defaultThickness;

        public string defaultTag = DEFAULT_TAG;
        public int defaultLayer;
        public int defaultSortingLayerId;
        public int defaultSortingOrder;

        public int maxThumbnailPreviewCount = THUMBNAIL_COUNT;
        
        public bool enableRenameMeshPrefabAutomatically;
    }
}