using System;
using System.Diagnostics.Eventing.Reader;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteProcessor : IDisposable
    {
        private readonly SpriteImportData _importData;
        private readonly SpritePreview _preview;

        private string _originalUserData;
        private SpriteConfigData _configData;
        private MeshCreatorBase _meshCreator;

        private bool _isDataChanged = false;
        private Object[] _targets;

        public bool IsTextureImporterMode { get; set; }

        public bool IsExtendedByEditorWindow { get; set; }

        public bool IsUIEnabled => !EditorWindow.HasOpenInstances<SpriteAssistEditorWindow>() || IsExtendedByEditorWindow;

        public bool IsEditorWindow => EditorWindow.HasOpenInstances<SpriteAssistEditorWindow>() && IsExtendedByEditorWindow;

        public SpriteProcessor(Sprite sprite, string assetPath)
        {
            _importData = new SpriteImportData(sprite, assetPath);
            _originalUserData = _importData.textureImporter.userData;
            _configData = SpriteConfigData.GetData(_originalUserData);
            _meshCreator = MeshCreatorBase.GetInstnace(_configData);
            _preview = new SpritePreview(_meshCreator.GetMeshWireframes());

            Undo.undoRedoPerformed += UndoReimport;
        }

        public void OnInspectorGUI()
        {
            _targets = Selection.objects;

            ShowCommonUI();

            if (IsUIEnabled)
            {
                ShowEnabledUI();
            }
            else
            {
                ShowDisabledUI();
            }
        }

        private void ShowCommonUI()
        {
            if (!IsEditorWindow && GUILayout.Button("Open with SpriteAssist EditorWindow"))
            {
                ShowSaveOrRevertUI();
                EditorWindow.GetWindow<SpriteAssistEditorWindow>();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PrefixLabel("Source Texture");
                EditorGUILayout.ObjectField(_importData.sprite.texture, typeof(Texture2D), false);
            }
        }

        private void ShowDisabledUI()
        {
            EditorGUILayout.HelpBox("SpriteAssist EditorWindow is already open.", MessageType.Info);
        }

        private void ShowEnabledUI()
        {
            using (var checkDataChange = new EditorGUI.ChangeCheckScope())
            {
                using (var checkModeChange = new EditorGUI.ChangeCheckScope())
                {
                    _configData.mode = (SpriteConfigData.Mode)EditorGUILayout.EnumPopup("SpriteAssist Mode", _configData.mode);
                    EditorGUILayout.Space();

                    if (checkModeChange.changed)
                    {
                        _meshCreator = MeshCreatorBase.GetInstnace(_configData);
                        _preview.SetWireframes(_meshCreator.GetMeshWireframes());
                    }
                }
                
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Mesh Settings");
                    }

                    if (_configData.mode.HasFlag(SpriteConfigData.Mode.TransparentMesh))
                    {
                        EditorGUILayout.LabelField("Transparent Mesh");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            _configData.transparentDetail = EditorGUILayout.Slider("Detail", _configData.transparentDetail, 0.001f, 1f);
                            _configData.transparentAlphaTolerance = (byte) EditorGUILayout.Slider("Alpha Tolerance", _configData.transparentAlphaTolerance, 0, 254);
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
                            _configData.opaqueAlphaTolerance = (byte) EditorGUILayout.Slider("Alpha Tolerance", _configData.opaqueAlphaTolerance, 0, 254);
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

                    if (_configData.mode == SpriteConfigData.Mode.UnityDefault)
                    {
                        using (new EditorGUILayout.VerticalScope(new GUIStyle {margin = new RectOffset(5, 5, 0, 5)}))
                            EditorGUILayout.HelpBox("Select other mode to use SpriteAssist.", MessageType.Info);
                    }

                    if (_configData.mode == SpriteConfigData.Mode.Complex)
                    {
                        using (new EditorGUILayout.VerticalScope(new GUIStyle {margin = new RectOffset(5, 5, 0, 5)}))
                            EditorGUILayout.HelpBox("Complex mode dose not override original sprite mesh.\nComplex mode only affects Mesh Prefab.", MessageType.Info);
                    }

                    _isDataChanged |= checkDataChange.changed;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Mesh Prefab");
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField("Prefab", _importData.MeshPrefab, typeof(GameObject), false);
                        string buttonText = _importData.HasMeshPrefab ? "Remove" : "Create";
                        if (GUILayout.Button(buttonText, GUILayout.Width(60)))
                        {
                            Apply(true, _importData.HasMeshPrefab);
                        }
                    }

                    if (!_importData.HasMeshPrefab)
                    {
                        Shader transparentShader = ShaderUtil.FindTransparentShader(_configData.transparentShaderName);
                        Shader opaqueShader = ShaderUtil.FindOpaqueShader(_configData.opaqueShaderName);
                        transparentShader = (Shader)EditorGUILayout.ObjectField("Transparent Shader", transparentShader, typeof(Shader), false);
                        opaqueShader = (Shader)EditorGUILayout.ObjectField("Opaque Shader", opaqueShader, typeof(Shader), false);
                        _configData.transparentShaderName = transparentShader == null ? null : transparentShader.name;
                        _configData.opaqueShaderName = opaqueShader == null ? null : opaqueShader.name;
                    }

                    EditorGUI.BeginChangeCheck();
                    _configData.thickness = EditorGUILayout.FloatField("Thickness", _configData.thickness);
                    _configData.thickness = Mathf.Max(0, _configData.thickness);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _isDataChanged = true;
                    }

                    EditorGUILayout.Space();

                    if (_configData != null && _configData.IsOverriden && _configData.mode == SpriteConfigData.Mode.Complex)
                    {
                        if (_importData.MeshPrefab == null)
                        {
                            using (new EditorGUILayout.VerticalScope(new GUIStyle { margin = new RectOffset(5, 0, 5, 5) }))
                                EditorGUILayout.HelpBox("To use complex mode must be created Mesh Prefab.", MessageType.Warning);
                        }
                    }
                }

                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(!_isDataChanged && !IsTextureImporterMode))
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

                if (!_importData.IsTightMesh)
                {
                    EditorGUILayout.HelpBox("Mesh Type is not Tight Mesh. Change texture setting.", MessageType.Warning);
                }

                EditorGUILayout.Space();

                if (checkDataChange.changed)
                {
                    Undo.RegisterCompleteObjectUndo(_importData.textureImporter, "SpriteAssist Texture");

                    _importData.textureImporter.userData = JsonUtility.ToJson(_configData);
                }
            }
        }

        public void OnPreviewGUI(Rect rect, Sprite sprite, TextureInfo textureInfo)
        {
            if (!IsUIEnabled)
            {
                return;
            }

            //skip 'rect (0, 0, 1, 1)' issue
            if (rect.width <= 1 || rect.height <= 1)
            {
                return;
            }

            //for multiple preview
            bool hasMultipleTargets = _targets.Length > 1;
            _preview.Show(rect, sprite, textureInfo, _configData, hasMultipleTargets);
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
                if (EditorUtility.DisplayDialog("Unapplied import settings", $"Unapplied import settings for '{_importData.assetPath}'", "Apply", "Revert"))
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
            _meshCreator = MeshCreatorBase.GetInstnace(_configData);
            _preview.SetWireframes(_meshCreator.GetMeshWireframes());
            _importData.textureImporter.userData = _originalUserData;
            _isDataChanged = false;
        }

        private void Apply(bool withMeshPrefabCreation = false, bool hasMeshPrefab = false)
        {
            Undo.RegisterCompleteObjectUndo(_targets, "SpriteAssist Texture");

            _originalUserData = JsonUtility.ToJson(_configData);
            
            foreach (var target in _targets)
            {
                Sprite sprite = null;

                switch (target)
                {
                    case Sprite value:
                        sprite = value;
                        break;

                    case Texture2D texture:
                        sprite = SpriteUtil.CreateDummySprite(texture);
                        break;

                    default:
                        continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(target);
                SpriteImportData importData = new SpriteImportData(sprite, assetPath);
                
                importData.textureImporter.userData = _originalUserData;

                EditorUtility.SetDirty(importData.textureImporter);
                AssetDatabase.WriteImportSettingsIfDirty(importData.textureImporter.assetPath);
                AssetDatabase.ImportAsset(importData.textureImporter.assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

                if (withMeshPrefabCreation)
                {
                    if (hasMeshPrefab)
                    {
                        importData.RemoveExternalPrefab();
                    }
                    else
                    {
                        importData.RemoveExternalPrefab();
                        TextureInfo textureInfo = new TextureInfo(importData.assetPath, importData.sprite);
                        GameObject prefab = _meshCreator.CreateExternalObject(importData.sprite, textureInfo, _configData);
                        importData.SetPrefabAsExternalObject(prefab);
                    }
                }
                else
                {
                    //update mesh prefab
                    if (importData.HasMeshPrefab)
                    {
                        PrefabUtil.CleanUpSubAssets(importData.MeshPrefab);
                        TextureInfo textureInfo = new TextureInfo(importData.assetPath, sprite);
                        string oldPrefabPath = AssetDatabase.GetAssetPath(importData.MeshPrefab);
                        GameObject prefab = _meshCreator.CreateExternalObject(sprite, textureInfo, _configData, oldPrefabPath);
                        importData.RemapExternalObject(prefab);
                    }
                } 

            }

            _isDataChanged = false;
        }

        private void UndoReimport()
        {
            _configData = SpriteConfigData.GetData(_importData.textureImporter.userData);
            _isDataChanged = true;

            foreach (var t in _targets)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(t), ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
            }
        }
    }
}
