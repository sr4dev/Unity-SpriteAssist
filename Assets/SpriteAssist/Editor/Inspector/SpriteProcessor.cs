using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteProcessor : IDisposable
    {
        public readonly SpriteImportData mainImportData;

        private readonly SpritePreview _preview;

        private static bool _isOpenMeshSettings = true;
        private static bool _isOpenMeshPrefab = true;

        private string _originalUserData;
        private SpriteConfigData _configData;
        private MeshCreatorBase _meshCreator;

        private bool _isDataChanged = false;

        private Object[] _targets;

        public SpriteProcessor(Sprite sprite, string assetPath)
        {
            mainImportData = new SpriteImportData(sprite, assetPath);
            _originalUserData = mainImportData.textureImporter.userData;
            _configData = SpriteConfigData.GetData(_originalUserData);
            _meshCreator = MeshCreatorBase.GetInstance(_configData.mode);
            _preview = new SpritePreview(_meshCreator.GetMeshWireframes());

            Undo.undoRedoPerformed += UndoReimport;
        }
        
        public void OnInspectorGUI(bool disableBaseGUI)
        {
            _targets = Selection.objects;

            if (!disableBaseGUI)
            {
                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PrefixLabel("Source Texture");
                    EditorGUILayout.ObjectField(mainImportData.sprite.texture, typeof(Texture2D), false);
                }
            }

            if (disableBaseGUI)
            {
                EditorGUI.indentLevel++;
            }

            using (var checkChangedMode = new EditorGUI.ChangeCheckScope())
            {
                _configData.mode = (SpriteConfigData.Mode) EditorGUILayout.EnumPopup("SpriteAssist Mode", _configData.mode);
                EditorGUILayout.Space();

                if (checkChangedMode.changed)
                {
                    _meshCreator = MeshCreatorBase.GetInstance(_configData.mode);
                    _preview.SetWireframes(_meshCreator.GetMeshWireframes());

                    //force apply and reimport
                    Apply();
                    return;
                }

                _isDataChanged |= checkChangedMode.changed;
            }

            if (disableBaseGUI)
            {
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel++;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    _isOpenMeshSettings = EditorGUILayout.Foldout(_isOpenMeshSettings, "Mesh Settings");
                }

                if (_isOpenMeshSettings)
                {
                    using (var checkChangedMeshSettings = new EditorGUI.ChangeCheckScope())
                    {
                        if (_configData.mode.HasFlag(SpriteConfigData.Mode.TransparentMesh))
                        {
                            EditorGUILayout.LabelField("Transparent Mesh");
                            using (new EditorGUI.IndentLevelScope())
                            {
                                _configData.transparentDetail = EditorGUILayout.Slider("Detail", _configData.transparentDetail, 0.001f, 1f);
                                _configData.transparentAlphaTolerance = (byte)EditorGUILayout.Slider("Alpha Tolerance", _configData.transparentAlphaTolerance, 0, 254);
                                _configData.detectHoles = EditorGUILayout.Toggle("Detect Holes", _configData.detectHoles);
                                EditorGUILayout.Space();
                            }
                        }

                        if (_configData.mode.HasFlag(SpriteConfigData.Mode.OpaqueMesh))
                        {
                            EditorGUILayout.LabelField("Opaque Mesh");
                            using (new EditorGUI.IndentLevelScope())
                            {
                                _configData.opaqueDetail = EditorGUILayout.Slider("Detail", _configData.opaqueDetail, 0.001f, 1f);
                                _configData.opaqueAlphaTolerance = (byte)EditorGUILayout.Slider("Alpha Tolerance", _configData.opaqueAlphaTolerance, 0, 254);
                                _configData.opaqueExtrude = EditorGUILayout.Slider("Extrude", _configData.opaqueExtrude, 0, 1);

                                using (new EditorGUI.DisabledScope(true))
                                {
                                    //force true
                                    EditorGUILayout.Toggle("Detect Holes (forced)", true);
                                }

                                EditorGUILayout.Space();
                            }
                        }

                        if (_configData.mode.HasFlag(SpriteConfigData.Mode.TransparentMesh) || _configData.mode.HasFlag(SpriteConfigData.Mode.OpaqueMesh))
                        {
                            _configData.edgeSmoothing = EditorGUILayout.Slider("Edge Smoothing", _configData.edgeSmoothing, 0f, 1f);
                            _configData.useNonZero = EditorGUILayout.Toggle("Non-zero Winding", _configData.useNonZero);
                            EditorGUILayout.Space();
                        }

                        if (_configData.mode == SpriteConfigData.Mode.UnityDefaultForTransparent || _configData.mode == SpriteConfigData.Mode.UnityDefaultForOpaque)
                        {
                            using (new EditorGUILayout.VerticalScope(new GUIStyle { margin = new RectOffset(5, 5, 0, 5) }))
                                EditorGUILayout.HelpBox("Select other mode to use SpriteAssist.", MessageType.Info);
                        }

                        if (_configData.mode == SpriteConfigData.Mode.Complex)
                        {
                            using (new EditorGUILayout.VerticalScope(new GUIStyle { margin = new RectOffset(5, 5, 0, 5) }))
                                EditorGUILayout.HelpBox("Complex mode dose not override original sprite mesh.\nComplex mode only affects Mesh Prefab.", MessageType.Info);
                        }

                        _isDataChanged |= checkChangedMeshSettings.changed;
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    _isOpenMeshPrefab = EditorGUILayout.Foldout(_isOpenMeshPrefab, "Mesh Prefab");
                }

                if (_isOpenMeshPrefab)
                {
                    using (var checkChangedMeshPrefab = new EditorGUI.ChangeCheckScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField("Prefab", mainImportData.MeshPrefab, typeof(GameObject), false);
                            string buttonText = mainImportData.HasMeshPrefab ? "Remove" : "Create";
                            if (GUILayout.Button(buttonText, GUILayout.Width(60)))
                            {
                                Apply(true, mainImportData.HasMeshPrefab);
                                return;
                            }
                        }

                        if (!mainImportData.HasMeshPrefab)
                        {
                            Shader transparentShader = ShaderUtil.FindTransparentShader(_configData.transparentShaderName);
                            Shader opaqueShader = ShaderUtil.FindOpaqueShader(_configData.opaqueShaderName);
                            transparentShader = (Shader)EditorGUILayout.ObjectField("Transparent Shader", transparentShader, typeof(Shader), false);
                            opaqueShader = (Shader)EditorGUILayout.ObjectField("Opaque Shader", opaqueShader, typeof(Shader), false);
                            _configData.transparentShaderName = transparentShader == null ? null : transparentShader.name;
                            _configData.opaqueShaderName = opaqueShader == null ? null : opaqueShader.name;
                        }

                        _configData.thickness = EditorGUILayout.FloatField("Thickness", _configData.thickness);
                        _configData.thickness = Mathf.Max(0, _configData.thickness);

                        EditorGUILayout.Space();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            _configData.overrideTag = EditorGUILayout.Toggle(_configData.overrideTag, GUILayout.Width(45));
                            GUILayout.Space(-30);

                            using (new EditorGUI.DisabledGroupScope(!_configData.overrideTag))
                            {
                                if (_configData.overrideTag)
                                {
                                    _configData.tag = EditorGUILayout.TagField("Tag", _configData.tag);
                                }
                                else
                                {
                                    EditorGUILayout.TagField("Tag", SpriteAssistSettings.Settings.defaultTag);
                                }
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            _configData.overrideLayer = EditorGUILayout.Toggle(_configData.overrideLayer, GUILayout.Width(45));
                            GUILayout.Space(-30);

                            using (new EditorGUI.DisabledGroupScope(!_configData.overrideLayer))
                            {
                                if (_configData.overrideLayer)
                                {
                                    _configData.layer = EditorGUILayout.LayerField("Layer", _configData.layer);
                                }
                                else
                                {
                                    EditorGUILayout.LayerField("Layer", SpriteAssistSettings.Settings.defaultLayer);
                                }
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            _configData.overrideSortingLayer = EditorGUILayout.Toggle(_configData.overrideSortingLayer, GUILayout.Width(45));
                            GUILayout.Space(-30);

                            using (new EditorGUI.DisabledGroupScope(!_configData.overrideSortingLayer))
                            {
                                if (_configData.overrideSortingLayer)
                                {
                                    int index = Array.FindIndex(SortingLayer.layers, layer => layer.id == _configData.sortingLayerId);
                                    index = EditorGUILayout.Popup("Sorting Layer", index, (from layer in SortingLayer.layers select layer.name).ToArray());
                                    _configData.sortingLayerId = SortingLayer.layers[index].id;
                                    _configData.sortingOrder = EditorGUILayout.IntField(_configData.sortingOrder, GUILayout.Width(60));
                                }
                                else
                                {
                                    int index = Array.FindIndex(SortingLayer.layers, layer => layer.id == SpriteAssistSettings.Settings.defaultSortingLayerId);
                                    EditorGUILayout.Popup("Sorting Layer", index, (from layer in SortingLayer.layers select layer.name).ToArray());
                                    EditorGUILayout.IntField(SpriteAssistSettings.Settings.defaultSortingOrder, GUILayout.Width(60));
                                }
                            }
                        }

                        EditorGUILayout.Space();

                        _isDataChanged |= checkChangedMeshPrefab.changed;
                    }
                    
                    EditorGUILayout.Space();

                    if (_configData != null && _configData.mode == SpriteConfigData.Mode.Complex)
                    {
                        if (mainImportData.MeshPrefab == null)
                        {
                            using (new EditorGUILayout.VerticalScope(new GUIStyle { margin = new RectOffset(5, 0, 5, 5) }))
                                EditorGUILayout.HelpBox("To use complex mode must be created Mesh Prefab.", MessageType.Warning);
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(!_isDataChanged))
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Revert", GUILayout.Width(50)))
                {
                    Revert();
                }

                if (GUILayout.Button("Apply", GUILayout.Width(50)))
                {
                    Apply();
                }
            }

            if (!mainImportData.IsTightMesh)
            {
                EditorGUILayout.HelpBox("Mesh Type is not Tight Mesh. Change texture setting.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            if (_isDataChanged)
            {
                Undo.RegisterCompleteObjectUndo(mainImportData.textureImporter, "SpriteAssist Texture");

                mainImportData.textureImporter.userData = JsonUtility.ToJson(_configData);
            }
        }

        public void OnPreviewGUI(Rect rect, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo)
        {
            //skip 'rect (0, 0, 1, 1)' issue
            if (rect.width <= 1 || rect.height <= 1)
            {
                return;
            }

            //for multiple preview
            bool hasMultipleTargets = Selection.objects.Length > 1;
            _preview.Show(rect, baseSprite, dummySprite, textureInfo, _configData, hasMultipleTargets);
        }

        public void Dispose()
        {
            _preview.Dispose();
            Undo.undoRedoPerformed -= UndoReimport;

            ShowSaveOrRevertUI();
        }

        private void ShowSaveOrRevertUI()
        {
            if (_isDataChanged)
            {
                if (EditorUtility.DisplayDialog("Unapplied import settings", $"Unapplied import settings for '{mainImportData.assetPath}'", "Apply", "Revert"))
                {
                    Apply();
                }
                else
                {
                    Revert();
                }
            }
        }

        private void Revert()
        {
            _configData = SpriteConfigData.GetData(_originalUserData);
            _meshCreator = MeshCreatorBase.GetInstance(_configData.mode);
            _preview.SetWireframes(_meshCreator.GetMeshWireframes());
            mainImportData.textureImporter.userData = _originalUserData;
            _isDataChanged = false;
        }

        public void Apply(bool withMeshPrefabProcess = false, bool hasMeshPrefab = false, bool withCopyFromSprite = false)
        {
            Undo.RegisterCompleteObjectUndo(_targets, "SpriteAssist Texture");

            _originalUserData = JsonUtility.ToJson(_configData);

            foreach (var selectedTarget in _targets)
            {
                Sprite sprite = null;

                switch (selectedTarget)
                {
                    case Sprite s:
                        sprite = s;
                        break;

                    case GameObject go:
                        if (go.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                        {
                            sprite = spriteRenderer.sprite;
                        }
                        else if (go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                        {
                            var path = AssetDatabase.GetAssetPath(meshRenderer.sharedMaterial.mainTexture);
                            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        }
                        break;
                }

                if (sprite == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(sprite);
                TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (textureImporter == null)
                {
                    continue;
                }

                SpriteImportData importData = new SpriteImportData(sprite, textureImporter, assetPath);
                importData.textureImporter.userData = _originalUserData;

                if (withMeshPrefabProcess)
                {
                    SetMeshPrefabContainer(importData, hasMeshPrefab);
                }

                if (withCopyFromSprite)
                {
                    Sprite rootSprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).FirstOrDefault(obj => obj is Sprite) as Sprite;

                    if (rootSprite != null)
                    {
                        textureImporter.spritePixelsPerUnit = rootSprite.pixelsPerUnit;
                        textureImporter.spritePivot = rootSprite.GetNormalizedPivot();
                    }
                }

                EditorUtility.SetDirty(importData.textureImporter);
                AssetDatabase.WriteImportSettingsIfDirty(importData.textureImporter.assetPath);
                importData.textureImporter.SaveAndReimport();
            }

            _isDataChanged = false;
        }

        public void SetMeshPrefabContainer(SpriteImportData importData, bool hasMeshPrefab)
        {
            if (hasMeshPrefab)
            {
                importData.RemoveExternalPrefab();
            }
            else
            {
                importData.RemoveExternalPrefab();
                TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
                GameObject prefab = _meshCreator.CreateExternalObject(importData.sprite, textureInfo, _configData);
                importData.SetPrefabAsExternalObject(prefab);
            }
        }

        public void UpdateSubAssetsInMeshPrefab(SpriteImportData importData)
        {
            //update mesh prefab
            if (importData.HasMeshPrefab)
            {
                //TODO prevent reimport loop
                if (EditorUtility.IsDirty(importData.textureImporter))
                {
                    EditorUtility.ClearDirty(importData.textureImporter);
                    return;
                }

                if (SpriteAssistSettings.Settings.enableRenameMeshPrefabAutomatically)
                {
                    PrefabUtil.TryRename(importData.assetPath, importData.MeshPrefab);
                }

                //importData.RemoveExternalPrefab();
                PrefabUtil.CleanUpSubAssets(importData.MeshPrefab);

                TextureInfo textureInfo = new TextureInfo(importData.sprite, importData.assetPath);
                _meshCreator.UpdateExternalObject(importData.MeshPrefab, importData.sprite, importData.dummySprite, textureInfo, _configData);
                importData.RemapExternalObject(importData.MeshPrefab);
                importData.textureImporter.SaveAndReimport();
                EditorUtility.SetDirty(importData.textureImporter);

            }
        }

        public void OverrideGeometry()
        {
            TextureInfo textureInfo = new TextureInfo(mainImportData.sprite, mainImportData.assetPath);
            _meshCreator.OverrideGeometry(mainImportData.sprite, mainImportData.dummySprite, textureInfo, _configData);
        }

        private void UndoReimport()
        {
            _configData = SpriteConfigData.GetData(mainImportData.textureImporter.userData);
            _isDataChanged = true;

            if (_targets == null)
                return;

            foreach (var t in _targets)
            {
                string path = AssetDatabase.GetAssetPath(t);

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                }
            }
        }
    }
}
