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
                    EditorGUILayout.LabelField("Mesh Prefab", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settings.FindProperty("defaultTransparentShaderName"), new GUIContent("Transparent Shader"));
                    EditorGUILayout.PropertyField(settings.FindProperty("defaultOpaqueShaderName"), new GUIContent("Opaque Shader"));
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Preview thumbnail", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settings.FindProperty("maxThumbnailPreviewCount"), new GUIContent("Max count"));
                    
                    settings.ApplyModifiedProperties();
                },
                keywords = new[] { "Sprite", "Assist", "SpriteAssist", "Shader" },
            };

            return provider;
        }
    }
}