using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

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

        [Tooltip("Controls if sprites are automatically or explicitly marked for processing")]
        public SpriteAssistInclusionMode inclusionMode;

        [Tooltip("Pattern matching globs to describe included/excluded files. All relative to Assets/.")]
        public string[] inclusionGlobs = {};

        protected Dictionary<string,Regex> compiledRegexes = new Dictionary<string, Regex>();

        public bool ShouldProcessSprite(Sprite s)
        {
            if(s == null)
            {
                return false;
            }
            string path = AssetDatabase.GetAssetPath(s);
            return ShouldProcessSprite(path);
        }
        
        public bool ShouldProcessSprite(string path)
        {
            if(string.IsNullOrEmpty(path) || !path.StartsWith("Assets"))
            {
                return false;
            }

            foreach(string glob in inclusionGlobs)
            {
                Regex r = GetRegex(glob);
                if(r.IsMatch(path))
                {
                    // Debug.Log($"[SpriteAssistSettings] {path} matches {glob}!");
                    // return true if explicit inclusion is selected, otherwise false for explicit exclusion
                    return inclusionMode == SpriteAssistInclusionMode.Include;
                }
                else
                {
                    // Debug.Log($"[SpriteAssistSettings] {path} does not match {glob}");
                }
            }
            // fallthru: return true if "include by default", otherwise false
            // Debug.Log($"[SpriteAssistSettings] No match for {path} found");
            return inclusionMode == SpriteAssistInclusionMode.Exclude;
        }

        protected void OnValidate()
        {
            // we don't have granular information to decide if the globs have updated
            // so just blow these away
            compiledRegexes.Clear();
        }

        private Regex GetRegex(string glob)
        {
            Regex result = null;
            if(!compiledRegexes.TryGetValue(glob, out result))
            {
                result = CompileGlob(glob);
                compiledRegexes.Add(glob, result);
            }
            return result;
        }

        private Regex CompileGlob(string glob)
        {
            glob = "Assets/" + glob;
            // Debug.Log($"[SpriteAssistSettings] compiling `${glob}`");
            return new Regex(
                "^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
            );
        }
    }
}
