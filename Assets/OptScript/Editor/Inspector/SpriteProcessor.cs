using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OptSprite
{
    public class SpriteProcessor
    {
        private SpriteImportData _importData;
        private SpriteConfigData _configData;
        private MeshPrefabCreator _meshCreator;
        private SpritePreview _preview;

        private bool _isDataChanged = false;
        private bool _needPreviewUpdate = true;

        private string _infoText = "TEST";

        public SpriteProcessor(Sprite sprite, string assetPath)
        {
            _importData = new SpriteImportData(sprite, assetPath);
            _configData = SpriteConfigData.GetData(_importData.textureImporter.userData);
            _meshCreator = MeshPrefabCreator.GetInstnace(_configData.mode);
            _preview = new SpritePreview(_configData.mode);

            Undo.undoRedoPerformed -= UndoReimport;
            Undo.undoRedoPerformed += UndoReimport;
        }

        public void OnInspectorGUI(Object target, Object[] targets)
        {
            using (var checkDataChange = new EditorGUI.ChangeCheckScope())
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    _configData.overriden = EditorGUILayout.ToggleLeft("Override Mesh", _configData.overriden, EditorStyles.boldLabel);
                }

                using (new EditorGUI.DisabledScope(!_configData.overriden))
                {
                    using (var checkModeChange = new EditorGUI.ChangeCheckScope())
                    {
                        _configData.mode = (SpriteConfigData.Mode)EditorGUILayout.EnumPopup("Method", _configData.mode);
                        EditorGUILayout.Space();

                        if (checkModeChange.changed)
                        {
                            _meshCreator = MeshPrefabCreator.GetInstnace(_configData.mode);
                            _preview.ChangeMode(_configData.mode);
                        }
                    }

                    if (_configData.mode.HasFlag(SpriteConfigData.Mode.TransparentMesh))
                    {
                        EditorGUILayout.LabelField("Transparent Mesh");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            _configData.detail = EditorGUILayout.Slider("Detail", _configData.detail, 0.001f, 1f);
                            _configData.alphaTolerance = (byte)EditorGUILayout.Slider("Alpha Tolerance", _configData.alphaTolerance, 1, 255);
                            _configData.detectHoles = EditorGUILayout.Toggle("Detect Holes", _configData.detectHoles);
                            EditorGUILayout.Space();
                        }
                    }

                    if (_configData.mode.HasFlag(SpriteConfigData.Mode.OpaqueMesh))
                    {
                        EditorGUILayout.LabelField("Opaque Mesh");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            _configData.opaqueAlphaTolerance = (byte)EditorGUILayout.Slider("Alpha Tolerance", _configData.opaqueAlphaTolerance, 1, 255);
                            _configData.vertexMergeDistance = (byte)EditorGUILayout.Slider("Merge Distance", _configData.vertexMergeDistance, 0, 30);
                            EditorGUILayout.Space();
                        }
                    }
                }

                _needPreviewUpdate |= checkDataChange.changed;
                _isDataChanged |= checkDataChange.changed;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
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

            if (_configData != null && _configData.overriden && _configData.mode == SpriteConfigData.Mode.Complex)
            {
                EditorGUILayout.HelpBox("To use complex mode must be created Mesh Prefab.", MessageType.Warning);
            }

            EditorGUILayout.LabelField("Transparent Mesh", _infoText, new GUILayoutOption[0]);

            EditorGUILayout.Space();
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
        }

        private void Clear()
        {
            _isDataChanged = false;
        }

        private void Apply(Object[] targets)
        {
            Dictionary<AssetImporter, Object> dictionary = targets.ToDictionary(t => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)));

            Undo.RegisterCompleteObjectUndo(dictionary.Values.ToArray(), "OptSprite Texture");

            foreach (KeyValuePair<AssetImporter, Object> kvp in dictionary)
            {
                AssetImporter importer = kvp.Key;
                Sprite _sprite = kvp.Value as Sprite;
                Mesh tMesh = null;
                Mesh oMesh = null;
                Material tMat = null;
                Material oMat = null;
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_importData.MeshPrefab));

                foreach (Object asset in allAssets)
                {
                    if (AssetDatabase.IsSubAsset(asset))
                    {
                        if (asset is Mesh)
                        {
                            if (asset.name == "Transparent")
                            {
                                //TODO
                                tMesh = (Mesh)asset;
                                MeshRenderType type = _configData.mode == SpriteConfigData.Mode.Complex ? MeshRenderType.SeparatedTransparent : MeshRenderType.Transparent;
                                SpriteUtil.UpdateMesh(_sprite, _configData, ref tMesh, type);
                            }
                            else if (asset.name == "Opaque")
                            {
                                //TODO
                                oMesh = (Mesh)asset;
                                SpriteUtil.UpdateMesh(_sprite, _configData, ref oMesh, MeshRenderType.Opaque);
                            }
                        }

                        if (asset is Material)
                        {
                            if (asset.name == "Transparent")
                            {
                                tMat = (Material)asset;
                                Shader shader = Shader.Find("Unlit/Transparent");
                                tMat.shader = shader;
                                tMat.SetTexture("_MainTex", _sprite.texture);
                            }
                            else if (asset.name == "Opaque")
                            {
                                oMat = (Material)asset;
                                Shader shader = Shader.Find("Unlit/Texture");
                                oMat.shader = shader;
                                oMat.SetTexture("_MainTex", _sprite.texture);
                            }
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
