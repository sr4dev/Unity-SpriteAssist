using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private Object _target;
        private Object[] _targets;

        public SpriteProcessor(Sprite sprite, string assetPath)
        {
            _importData = new SpriteImportData(sprite, assetPath);
            _originalUserData = _importData.textureImporter.userData;
            _configData = SpriteConfigData.GetData(_originalUserData);
            _meshCreator = MeshCreatorBase.GetInstnace(_configData);
            _preview = new SpritePreview(_meshCreator.GetMeshWireframes());

            Undo.undoRedoPerformed += UndoReimport;
        }

        public void OnInspectorGUI(Object target, Object[] targets)
        {
            _target = target;
            _targets = targets;

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PrefixLabel("Source Texture");
                EditorGUILayout.ObjectField(_importData.sprite.texture, typeof(Texture2D), false);
            }

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
                            Apply();

                            if (_importData.HasMeshPrefab)
                            {
                                _importData.RemoveExternalPrefab();
                            }
                            else
                            {
                                var prefab = _meshCreator.CreateExternalObject(_importData.sprite, _configData);
                                _importData.SetPrefabAsExternalObject(prefab);
                            }
                        }
                    }

                    if (!_importData.HasMeshPrefab)
                    {
                        Shader transparentShader = ShaderUtil.FindTransparentShader(_configData.transparentShaderName);
                        Shader opaqueShader = ShaderUtil.FindOpaqueShader(_configData.opaqueShaderName);
                        transparentShader = (Shader)EditorGUILayout.ObjectField("Transparent Shader", transparentShader, typeof(Shader), false);
                        opaqueShader = (Shader)EditorGUILayout.ObjectField("Opaque Shader", opaqueShader, typeof(Shader), false);
                        _configData.transparentShaderName = transparentShader?.name;
                        _configData.opaqueShaderName = opaqueShader?.name;
                    }

                    EditorGUI.BeginChangeCheck();
                    _configData.thickness = EditorGUILayout.FloatField("Thickness", _configData.thickness);
                    _configData.thickness = Mathf.Max(0, _configData.thickness);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _isDataChanged |= true;
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

        public void OnPreviewGUI(Rect rect, Object target)
        {
            //skip 'rect (0, 0, 1, 1)' issue
            if (rect.width <= 1 || rect.height <= 1)
            {
                return;
            }

            //for mulriple preview
            Sprite sprite = (Sprite)target;
            bool hasMultipleTargets = _targets.Length > 1;
            _preview.Show(rect, sprite, _configData, hasMultipleTargets);
        }

        public void Dispose()
        {
            _preview.Dispose();
            Undo.undoRedoPerformed -= UndoReimport;

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

        private void Apply()
        {
            Undo.RegisterCompleteObjectUndo(_targets, "SpriteAssist Texture");

            _originalUserData = JsonUtility.ToJson(_configData);

            Dictionary<AssetImporter, Object> dictionary = _targets.ToDictionary(t => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)));

            foreach (KeyValuePair<AssetImporter, Object> kvp in dictionary)
            {
                AssetImporter importer = kvp.Key;
                Sprite sprite = kvp.Value as Sprite;
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_importData.MeshPrefab));

                foreach (Object asset in allAssets)
                {
                    if (AssetDatabase.IsSubAsset(asset) && asset is Mesh mesh)
                    {
                        if (asset.name == MeshCreatorBase.RENDER_TYPE_TRANSPARENT)
                        {
                            MeshRenderType meshRenderType = _configData.mode == SpriteConfigData.Mode.Complex ? MeshRenderType.SeparatedTransparent : MeshRenderType.Transparent;
                            sprite.GetVertexAndTriangle3D(_configData, out var v, out var t, meshRenderType);
                            sprite.UpdateMesh(ref mesh, v, t);
                        }
                        else if (asset.name == MeshCreatorBase.RENDER_TYPE_OPAQUE)
                        {
                            sprite.GetVertexAndTriangle3D(_configData, out var v, out var t, MeshRenderType.Opaque);
                            sprite.UpdateMesh(ref mesh, v, t);
                        }
                    }
                }

                importer.userData = _originalUserData;

                EditorUtility.SetDirty(importer);
                AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
                AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
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
