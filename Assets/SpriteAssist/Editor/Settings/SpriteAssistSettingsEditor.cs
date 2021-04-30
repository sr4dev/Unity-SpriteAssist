using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class SpriteAssistSettingsEditor
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Project/SpriteAssist", SettingsScope.Project)
            {
                label = "SpriteAssist",
                guiHandler = (_) =>
                {
                    SerializedObject settings = SpriteAssistSettings.GetSerializedSettings();
                    EditorGUILayout.Space();
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("Mesh Prefab", EditorStyles.boldLabel);

                        EditorGUILayout.LabelField("Prefab File");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(settings.FindProperty("prefabNamePrefix"), new GUIContent("File Name Prefix"));
                            EditorGUILayout.PropertyField(settings.FindProperty("prefabNameSuffix"), new GUIContent("File Name Suffix"));
                            EditorGUILayout.PropertyField(settings.FindProperty("prefabRelativePath"), new GUIContent("Relative Path"));
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("Default Shader");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(settings.FindProperty("defaultTransparentShaderName"), new GUIContent("Transparent"));
                            EditorGUILayout.PropertyField(settings.FindProperty("defaultOpaqueShaderName"), new GUIContent("Opaque"));
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("Preview thumbnail", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(settings.FindProperty("maxThumbnailPreviewCount"), new GUIContent("Max count"));
                        EditorGUILayout.Space();
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("GitHub", GUILayout.Width(100)))
                        {
                            Application.OpenURL("https://github.com/sr4dev/Unity-SpriteAssist");
                        }

                        EditorGUILayout.Space();
                    }

                    settings.ApplyModifiedProperties();
                },
                keywords = new[] { "Sprite", "Assist", "SpriteAssist", "Shader" },
            };

            return provider;
        }
    }
}