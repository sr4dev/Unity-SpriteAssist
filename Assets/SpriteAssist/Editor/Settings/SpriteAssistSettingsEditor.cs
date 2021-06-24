using System;
using System.Linq;
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
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.Settings.prefabNamePrefix)), new GUIContent("File Name Prefix"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.Settings.prefabNameSuffix)), new GUIContent("File Name Suffix"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.Settings.prefabRelativePath)), new GUIContent("Relative Path"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.Settings.enableRenameMeshPrefabAutomatically)), new GUIContent("Auto Rename", "Rename prefab when renamed texture asset"));
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("Default Shader");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.Settings.defaultTransparentShaderName)), new GUIContent("Transparent"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.Settings.defaultOpaqueShaderName)), new GUIContent("Opaque"));
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("Tags and Layers");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            var tagProperty = settings.FindProperty(nameof(SpriteAssistSettings.Settings.defaultTag));
                            var layerProperty = settings.FindProperty(nameof(SpriteAssistSettings.Settings.defaultLayer));
                            var sortingLayerProperty = settings.FindProperty(nameof(SpriteAssistSettings.Settings.defaultSortingLayerId));
                            var sortingOrder = settings.FindProperty(nameof(SpriteAssistSettings.Settings.defaultSortingOrder));

                            tagProperty.stringValue = EditorGUILayout.TagField("Tag", tagProperty.stringValue);
                            layerProperty.intValue = EditorGUILayout.LayerField("Layer", layerProperty.intValue);
                            
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                sortingOrder.intValue = EditorGUILayout.IntField("Sorting Layer", sortingOrder.intValue);
                                GUILayout.Space(-20);
                                int index = Array.FindIndex(SortingLayer.layers, layer => layer.id == sortingLayerProperty.intValue);
                                index = EditorGUILayout.Popup(index, (from layer in SortingLayer.layers select layer.name).ToArray());
                                sortingLayerProperty.intValue = SortingLayer.layers[index].id;
                            }

                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("Preview thumbnail", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.Settings.maxThumbnailPreviewCount)), new GUIContent("Max count"));
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