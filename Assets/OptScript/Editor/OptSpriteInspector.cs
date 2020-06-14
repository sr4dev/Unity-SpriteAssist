using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class OptSpriteInspector : UnitySpriteInspector
    {
        private const string WIRE_FRAME_SHADER_NAME = "Unlit/Transparent";
        private static readonly Color _wireFrameColor = new Color(0.0f, 0.0f, 1.0f, 0.3f);
        private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

        private Material _previewMeshLineMaterial;
        private OptSpriteData _data;
        private bool _isChanged;

        protected override void OnEnable()
        {
            base.OnEnable();

            CreateMeshLineMaterial();
            Undo.undoRedoPerformed -= UndoReimport;
            Undo.undoRedoPerformed += UndoReimport;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            DestroyMeshLineMaterial();
        }

        public override void OnInspectorGUI()
        {
            string assetPath = AssetDatabase.GetAssetPath(target);
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            bool isTightMesh = textureImporterSettings.spriteMeshType == SpriteMeshType.Tight;
            bool isSingleSprite = textureImporterSettings.spriteMode == 1;

            if (isTightMesh == false || isSingleSprite == false)
            {
                string message = "OptSprite\n";
                if (isTightMesh)
                {
                    message += "Mesh Type is not Tight Mesh. ";
                }

                if (isSingleSprite)
                {
                    message += "Sprite Mode is not Single.";
                }

                EditorGUILayout.HelpBox(message, MessageType.Warning);

                base.OnInspectorGUI();
                return;
            }

            if (_data == null)
            {
                _data = OptSpriteData.GetData(textureImporter.userData);
            }

            //GUIStyle guiStyle = new GUIStyle(EditorStyles.helpBox);
            //guiStyle.padding = new RectOffset(10, 10, 10, 10);
            //EditorGUILayout.BeginVertical();
            EditorGUILayout.HelpBox("OptSprite Enabled", MessageType.None);

            bool oldEnabled = GUI.enabled;

            EditorGUI.BeginChangeCheck();
            _data.overriden = EditorGUILayout.ToggleLeft("Override Mesh", _data.overriden);
            GUI.enabled = _data.overriden;
            _data.detail = EditorGUILayout.Slider("Detail", _data.detail, 0.001f, 1f);
            _data.alphaTolerance = (byte)EditorGUILayout.Slider("Alpha Tolerance", _data.alphaTolerance, 0, 254);
            //_data.vertexMergeDistance = (byte)EditorGUILayout.Slider("Merge Distance", _data.vertexMergeDistance, 0, 30);
            _data.detectHoles = EditorGUILayout.Toggle("Detect Holes", _data.detectHoles);
            _isChanged |= EditorGUI.EndChangeCheck();

            GUI.enabled = _isChanged;

            if (GUILayout.Button("Revert"))
            {
                _isChanged = false;
                _data = null;
            }

            if (GUILayout.Button("Apply"))
            {
                AssetImporter[] assetImporters = targets
                    .Select(t => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)))
                    .Where(ai => ai != null)
                    .ToArray();

                Undo.RegisterCompleteObjectUndo(assetImporters, "OptSprite Texture");

                foreach (var currentAssetImporter in assetImporters)
                {
                    currentAssetImporter.userData = JsonUtility.ToJson(_data);

                    EditorUtility.SetDirty(currentAssetImporter);
                    AssetDatabase.WriteImportSettingsIfDirty(currentAssetImporter.assetPath);
                    AssetDatabase.ImportAsset(currentAssetImporter.assetPath,
                        ImportAssetOptions.ForceUpdate |
                        ImportAssetOptions.DontDownloadFromCacheServer);
                }

                _isChanged = false;
                _data = null;
            }

            GUI.enabled = oldEnabled;

            //EditorGUILayout.EndVertical();
            base.OnInspectorGUI();
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            base.OnPreviewGUI(rect, background);

            Sprite sprite = target as Sprite;

            if (sprite != null)
            {
                DrawSpriteWireframe(rect, sprite, _data);
            }
        }

        private void UndoReimport()
        {
            _data = null;

            foreach (var t in targets)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(t),
                    ImportAssetOptions.ForceUpdate |
                    ImportAssetOptions.DontDownloadFromCacheServer);
            }
        }

        private void DrawSpriteWireframe(Rect rect, Sprite sprite, OptSpriteData data)
        {
            Rect sRect = sprite.rect;
            float spriteMinScale = SpriteUtil.GetMinRectScale(rect, sRect);
            Vector2 previewPosition = rect.center - (sRect.size * spriteMinScale * 0.5f);
            SpriteUtil.GetMeshData(sprite, data, out var previewVertices, out var previewTriangles);
            SpriteUtil.GetScaledVertices(previewVertices, sprite.pixelsPerUnit, sprite.pivot, sprite.rect.size, spriteMinScale, true, false);

            _previewMeshLineMaterial.SetPass(0);
            DrawMesh(previewPosition, previewVertices, previewTriangles, false);
            DrawMesh(previewPosition, previewVertices, previewTriangles, true);
            Debug.Log($"vert {previewVertices.Length}, tri {previewTriangles.Length}");
        }

        private void DrawMesh(Vector2 pos, Vector2[] vertices, ushort[] triangles, bool isWireframe)
        {
            GL.wireframe = isWireframe;
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.Translate(pos));
            GL.Begin(GL.TRIANGLES);

            foreach (ushort t in triangles)
            {
                GL.Vertex3(vertices[t].x, vertices[t].y, 0f);
            }

            GL.End();
            GL.PopMatrix();
            GL.wireframe = false;
        }

        private void CreateMeshLineMaterial()
        {
            DestroyMeshLineMaterial();

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, _wireFrameColor);
            texture.Apply();

            _previewMeshLineMaterial = new Material(Shader.Find(WIRE_FRAME_SHADER_NAME));
            _previewMeshLineMaterial.SetTexture(_mainTex, texture);
            _previewMeshLineMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        private void DestroyMeshLineMaterial()
        {
            if (_previewMeshLineMaterial != null)
            {
                DestroyImmediate(_previewMeshLineMaterial);
            }
        }
    }
}
