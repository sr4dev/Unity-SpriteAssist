using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    [FilePath(SETTINGS_PATH, FilePathAttribute.Location.ProjectFolder)]
    public class SpriteAssistSettings : ScriptableSingleton<SpriteAssistSettings>
    {
        private const string SETTINGS_PATH = "ProjectSettings/SpriteAssistSettings.asset";
        private const string LEGACY_SETTINGS_PATH = "Assets/Editor/SpriteAssistSettings.asset";
        private const string RENDER_SHADER_TRANSPARENT = "Unlit/Transparent";
        private const string RENDER_SHADER_OPAQUE = "Unlit/Texture";
        public const string DEFAULT_TAG = "Untagged";
        private const int THUMBNAIL_COUNT = 10;

        public string prefabNamePrefix;
        public string prefabNameSuffix;
        public string prefabRelativePath;

        public string defaultTransparentShaderName = RENDER_SHADER_TRANSPARENT;
        public string defaultOpaqueShaderName = RENDER_SHADER_OPAQUE;
        public TriangulationLibrary defaultTriangulationLibrary = TriangulationLibrary.LibTessDotNet;
        public bool logTriangulationFallback = true;
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

        [System.NonSerialized]
        protected Dictionary<string,Regex> compiledRegexes = new Dictionary<string, Regex>();

        [InitializeOnLoadMethod]
        private static void MigrateLegacySettings()
        {
            if (!File.Exists(LEGACY_SETTINGS_PATH))
            {
                return;
            }

            bool migratedValues = false;
            if (!File.Exists(SETTINGS_PATH))
            {
                File.Copy(LEGACY_SETTINGS_PATH, SETTINGS_PATH);
                migratedValues = true;
            }

            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(LEGACY_SETTINGS_PATH))
                {
                    return;
                }

                try
                {
                    AssetDatabase.DeleteAsset(LEGACY_SETTINGS_PATH);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SpriteAssist] Failed to delete legacy settings asset '{LEGACY_SETTINGS_PATH}'. Will retry on next domain reload. {e}");
                    return;
                }

                if (migratedValues)
                {
                    Debug.Log($"[SpriteAssist] Migrated settings from '{LEGACY_SETTINGS_PATH}' to '{SETTINGS_PATH}'.");
                }
                else
                {
                    Debug.Log($"[SpriteAssist] Removed leftover legacy settings asset '{LEGACY_SETTINGS_PATH}'.");
                }
            };
        }

        public bool ShouldProcessSprite(Sprite s)
        {
            if(s == null)
            {
                return false;
            }
            string path = AssetDatabase.GetAssetPath(s);
            return ShouldProcessSprite(path);
        }

        public void SaveSettings()
        {
            Save(true);
        }

        // In parallel import workers ScriptableSingleton.instance does not reflect changes made in the main process and returns a stale value,
        // so the triangulation library (which affects the mesh result) is read directly from the file during import to keep worker and main results consistent.
        public static TriangulationLibrary ResolvedDefaultTriangulationLibrary
        {
            get
            {
                if (PrefabUtil.IsAssetImportWorkerProcess())
                {
                    try
                    {
                        if (File.Exists(SETTINGS_PATH))
                        {
                            foreach (string raw in File.ReadAllLines(SETTINGS_PATH))
                            {
                                string line = raw.Trim();
                                if (line.StartsWith("defaultTriangulationLibrary:") &&
                                    int.TryParse(line.Substring("defaultTriangulationLibrary:".Length).Trim(), out int value))
                                {
                                    return (TriangulationLibrary)value;//Todo: more simple way to find the value of defaultTriangulationLibrary in the settings file?
                                }
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                    }
                }

                return instance.defaultTriangulationLibrary;
            }
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
