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
                    EditorGUILayout.LabelField("Default Shader Name", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settings.FindProperty("defaultTransparentShaderName"), new GUIContent("Transparent"));
                    EditorGUILayout.PropertyField(settings.FindProperty("defaultOpaqueShaderName"), new GUIContent("Opaque"));
                    settings.ApplyModifiedProperties();
                },
                keywords = new[] { "Sprite", "Assist", "SpriteAssist", "Shader" },
            };

            return provider;
        }
    }
}