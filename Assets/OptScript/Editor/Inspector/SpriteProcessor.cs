using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OptSprite
{
    public class SpriteProcessor : IDisposable
    {
        private readonly SpriteImportData _importData;
        private readonly SpritePreview _preview;

        private SpriteConfigData _configData;
        private MeshCreator _meshCreator;

        private bool _isDataChanged = false;
        private bool _needPreviewUpdate = true;

        public SpriteProcessor(Sprite sprite, string assetPath)
        {
            _importData = new SpriteImportData(sprite, assetPath);
            _configData = SpriteConfigData.GetData(_importData.textureImporter.userData);
            _meshCreator = MeshCreator.GetInstnace(_configData);
            _preview = new SpritePreview(_meshCreator.GetMeshWireframes());

            Undo.undoRedoPerformed += UndoReimport;
        }

        public void OnInspectorGUI(Object target, Object[] targets)
        {
            using (var checkDataChange = new EditorGUI.ChangeCheckScope())
            {
                _configData.overriden = EditorGUILayout.ToggleLeft("Enable SpriteAssist", _configData.overriden);
                EditorGUILayout.Space();

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Mesh Settings");
                    }

                    using (new EditorGUI.DisabledScope(!_configData.overriden))
                    {
                        using (var checkModeChange = new EditorGUI.ChangeCheckScope())
                        {
                            _configData.mode = (SpriteConfigData.Mode)EditorGUILayout.EnumPopup("Mode", _configData.mode);
                            //_configData.windingRule = (WindingRule)EditorGUILayout.EnumPopup("Winding Rule", _configData.windingRule);//TODO
                            EditorGUILayout.Space();

                            if (checkModeChange.changed)
                            {
                                _meshCreator = MeshCreator.GetInstnace(_configData);
                                _preview.SetWireframes(_meshCreator.GetMeshWireframes());
                            }
                        }

                        if (_configData.mode.HasFlag(SpriteConfigData.Mode.TransparentMesh))
                        {
                            EditorGUILayout.LabelField("Transparent Mesh");
                            using (new EditorGUI.IndentLevelScope())
                            {
                                _configData.transparentDetail = EditorGUILayout.Slider("Detail", _configData.transparentDetail, 0.001f, 1f);
                                _configData.transparentAlphaTolerance = (byte)EditorGUILayout.Slider("Alpha Tolerance", _configData.transparentAlphaTolerance, 1, 255);
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
                                _configData.opaqueAlphaTolerance = (byte)EditorGUILayout.Slider("Alpha Tolerance", _configData.opaqueAlphaTolerance, 1, 255);
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    //force true
                                    EditorGUILayout.Toggle("Detect Holes (forced)", true);
                                }
                                EditorGUILayout.Space();
                            }
                        }

                        _configData.edgeSmoothing = EditorGUILayout.Slider("Edge Smoothing", _configData.edgeSmoothing, 0f, 1f);
                        EditorGUILayout.Space();
                    }

                    if (_configData != null && _configData.overriden && _configData.mode == SpriteConfigData.Mode.Complex)
                    {
                        using (new EditorGUILayout.VerticalScope(new GUIStyle { margin = new RectOffset(5, 5, 0, 5) }))
                            EditorGUILayout.HelpBox("Complex mode dose not override original sprite mesh.", MessageType.Info);
                    }

                    _needPreviewUpdate |= checkDataChange.changed;
                    _isDataChanged |= checkDataChange.changed;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Advanced");
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField("Mesh Prefab", _importData.MeshPrefab, typeof(GameObject), false);

                        string buttonText = _importData.HasMeshPrefab ? "Remove" : "Create";
                        if (GUILayout.Button(buttonText, GUILayout.Width(60)))
                        {
                            Apply(targets);

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

                    EditorGUILayout.Space();

                    if (_configData != null && _configData.overriden && _configData.mode == SpriteConfigData.Mode.Complex)
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
                        Clear();
                    }

                    if (GUILayout.Button("Apply", GUILayout.Width(50)))
                    {
                        Apply(targets);
                    }
                }

                if (!_importData.IsTightMesh)
                {
                    EditorGUILayout.HelpBox("Mesh Type is not Tight Mesh. Change texture setting.", MessageType.Warning);
                }

                EditorGUILayout.Space();
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
            _preview.Show(rect, sprite, _configData, _needPreviewUpdate);
            _needPreviewUpdate = false;
        }

        public void Dispose()
        {
            _preview.Dispose();

            Undo.undoRedoPerformed -= UndoReimport;
        }

        private void Clear()
        {
            _isDataChanged = false;
        }

        private void Apply(Object[] targets)
        {
            Dictionary<AssetImporter, Object> dictionary = targets.ToDictionary(t => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)));

            Undo.RegisterCompleteObjectUndo(targets, "OptSprite Texture");

            foreach (KeyValuePair<AssetImporter, Object> kvp in dictionary)
            {
                AssetImporter importer = kvp.Key;
                Sprite sprite = kvp.Value as Sprite;
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_importData.MeshPrefab));

                foreach (Object asset in allAssets)
                {
                    if (!AssetDatabase.IsSubAsset(asset))
                    {
                        continue;
                    }

                    if (asset is Mesh mesh)
                    {
                        if (asset.name == MeshCreator.RENDER_TYPE_TRANSPARENT)
                        {
                            MeshRenderType meshRenderType = _configData.mode == SpriteConfigData.Mode.Complex ? MeshRenderType.SeparatedTransparent : MeshRenderType.Transparent;
                            sprite.GetMeshData(_configData, out var v, out var t, meshRenderType);
                            sprite.UpdateMesh(ref mesh, v, t);
                        }
                        else if (asset.name == MeshCreator.RENDER_TYPE_OPAQUE)
                        {
                            sprite.GetMeshData(_configData, out var v, out var t, MeshRenderType.Opaque);
                            sprite.UpdateMesh(ref mesh, v, t);
                        }
                    }

                    if (asset is Material mat)
                    {
                        if (asset.name == MeshCreator.RENDER_TYPE_TRANSPARENT)
                        {
                            mat.shader = Shader.Find(MeshCreator.RENDER_SHADER_TRANSPARENT);
                            mat.SetTexture("_MainTex", sprite.texture);
                        }
                        else if (asset.name == MeshCreator.RENDER_TYPE_OPAQUE)
                        {
                            mat.shader = Shader.Find(MeshCreator.RENDER_SHADER_OPAQUE);
                            mat.SetTexture("_MainTex", sprite.texture);
                        }
                    }
                }

                importer.userData = JsonUtility.ToJson(_configData);

                EditorUtility.SetDirty(importer);
                AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
                AssetDatabase.ImportAsset(importer.assetPath,
                    ImportAssetOptions.ForceUpdate |
                    ImportAssetOptions.DontDownloadFromCacheServer);
            }

            Clear();
        }


        private void UndoReimport()
        {
            _configData = null;

            //foreach (var t in targets)
            //{
            //    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(t),
            //        ImportAssetOptions.ForceUpdate |
            //        ImportAssetOptions.DontDownloadFromCacheServer);
            //}
        }
    }
}
