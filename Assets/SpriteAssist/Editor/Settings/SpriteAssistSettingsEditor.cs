using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class SpriteAssistSettingsEditor
    {
        private const string IgnoreLibraryChangeDialogKey = "SpriteAssist.IgnoreLibraryChangeDialog";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Project/SpriteAssist", SettingsScope.Project)
            {
                label = "SpriteAssist",
                guiHandler = (_) =>
                {
                    SerializedObject settings = new SerializedObject(SpriteAssistSettings.instance);
                    EditorGUILayout.Space();

                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("Mesh Prefab", EditorStyles.boldLabel);

                        EditorGUILayout.LabelField("Prefab File");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.instance.prefabNamePrefix)), new GUIContent("File Name Prefix"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.instance.prefabNameSuffix)), new GUIContent("File Name Suffix"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.instance.prefabRelativePath)), new GUIContent("Relative Path"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically)), new GUIContent("Auto Rename", "Rename prefab when renamed texture asset"));
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("Default Shader");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.instance.defaultTransparentShaderName)), new GUIContent("Transparent"));
                            EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.instance.defaultOpaqueShaderName)), new GUIContent("Opaque"));
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.LabelField("Tags and Layers");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            var tagProperty = settings.FindProperty(nameof(SpriteAssistSettings.instance.defaultTag));
                            var layerProperty = settings.FindProperty(nameof(SpriteAssistSettings.instance.defaultLayer));
                            var sortingLayerProperty = settings.FindProperty(nameof(SpriteAssistSettings.instance.defaultSortingLayerId));
                            var sortingOrder = settings.FindProperty(nameof(SpriteAssistSettings.instance.defaultSortingOrder));

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
                        EditorGUILayout.PropertyField(settings.FindProperty(nameof(SpriteAssistSettings.instance.maxThumbnailPreviewCount)), new GUIContent("Max count"));
                        EditorGUILayout.Space();

                        EditorGUILayout.LabelField("Library", EditorStyles.boldLabel);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            SerializedProperty libraryProperty = settings.FindProperty(nameof(SpriteAssistSettings.instance.defaultTriangulationLibrary));
                            TriangulationLibrary[] libraryOrder = { TriangulationLibrary.IShape, TriangulationLibrary.LibTessDotNet };
                            string[] libraryOptions = libraryOrder.Select(library => TriangulationUtil.GetTriangulator(library).DisplayName).ToArray();
                            int libraryDisplayIndex = Array.IndexOf(libraryOrder, (TriangulationLibrary)libraryProperty.enumValueIndex);

                            EditorGUI.BeginChangeCheck();
                            libraryDisplayIndex = EditorGUILayout.Popup(new GUIContent("Triangulation"), libraryDisplayIndex, libraryOptions);
                            if (EditorGUI.EndChangeCheck())
                            {
                                int newLibraryEnum = (int)libraryOrder[libraryDisplayIndex];

                                if (SessionState.GetBool(IgnoreLibraryChangeDialogKey, false))
                                {
                                    libraryProperty.enumValueIndex = newLibraryEnum;
                                }
                                else
                                {
                                    string fromLibraryName = TriangulationUtil.GetTriangulator((TriangulationLibrary)libraryProperty.enumValueIndex).DisplayName;
                                    string toLibraryName = TriangulationUtil.GetTriangulator((TriangulationLibrary)newLibraryEnum).DisplayName;

                                    int choice = EditorUtility.DisplayDialogComplex(
                                        "Change Triangulation Library",
                                        $"Library will change from \"{fromLibraryName}\" to \"{toLibraryName}\".\n\n" +
                                        "This only applies to newly generated meshes. Existing meshes need a manual reimport (or Reimport All) to update.",
                                        "Change and Reimport All",
                                        "Just Change Library (ignore until Unity restart)",
                                        "Just Change Library");

                                    switch (choice)
                                    {
                                        case 0:
                                            libraryProperty.enumValueIndex = newLibraryEnum;
                                            settings.ApplyModifiedProperties();
                                            EditorApplication.ExecuteMenuItem("Assets/Reimport All");
                                            break;

                                        case 1:
                                            libraryProperty.enumValueIndex = newLibraryEnum;
                                            SessionState.SetBool(IgnoreLibraryChangeDialogKey, true);
                                            break;

                                        default:
                                            libraryProperty.enumValueIndex = newLibraryEnum;
                                            break;
                                    }
                                }
                            }

                            EditorGUILayout.HelpBox(TriangulationUtil.GetTriangulator((TriangulationLibrary)libraryProperty.enumValueIndex).Description, MessageType.Info);

                            EditorGUILayout.Space();
                        }
                    }
                    
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("Links", EditorStyles.boldLabel);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(EditorGUI.indentLevel * 15f);

                            if (GUILayout.Button("GitHub", GUILayout.Width(100)))
                            {
                                Application.OpenURL("https://github.com/sr4dev/Unity-SpriteAssist");
                            }

                            if (GUILayout.Button("OpenUPM", GUILayout.Width(100)))
                            {
                                Application.OpenURL("https://openupm.com/packages/com.sr4dev.unity-spriteassist/");
                            }

                            GUILayout.FlexibleSpace();
                        }
                    }

                    settings.ApplyModifiedProperties();
                },
                keywords = new[] { "Sprite", "Assist", "SpriteAssist", "Shader" },
            };

            return provider;
        }
    }
}
