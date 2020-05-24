using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class OptSpriteInspector : UnitySpriteInspector
    {
        private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

        private Material _previewMeshLineMaterial;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            CreateMeshLineMaterial();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            DestroyMeshLineMaterial();
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            base.OnPreviewGUI(rect, background);

            Sprite sprite = target as Sprite;

            if (sprite != null)
            {
                DrawSpriteWireframe(rect, sprite);
            }
        }

        private void DrawSpriteWireframe(Rect rect, Sprite sprite)
        {
            float spriteMinScale = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
            float previewPixelsPerUnit = sprite.pixelsPerUnit * spriteMinScale;
            Vector2 previewPivot = sprite.pivot * spriteMinScale;
            Vector2 previewSize = sprite.rect.size * spriteMinScale;
            Vector2 previewPosition = rect.center - previewSize * 0.5f;
            Vector2[] previewVertices = sprite.vertices;
            ushort[] previewTriangles = sprite.triangles;

            for (int i = 0; i < previewVertices.Length; i++)
            {
                Vector2 vertex = previewVertices[i] * previewPixelsPerUnit + previewPivot;
                vertex.y = (vertex.y - previewSize.y) * -1.0f;
                previewVertices[i] = vertex;
            }

            _previewMeshLineMaterial.SetPass(0);
            DrawMesh(previewPosition, previewVertices, previewTriangles, false);
            DrawMesh(previewPosition, previewVertices, previewTriangles, true);
        }

        private void DrawMesh(Vector2 pos, Vector2[] vertices, ushort[] triangles, bool isWireframe)
        {
            if (isWireframe)
            {
                GL.wireframe = true;
            }

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one));
            GL.Begin(GL.TRIANGLES);

            foreach (ushort tri in triangles)
            {
                GL.Vertex3(vertices[tri].x, vertices[tri].y, 0f);
            }

            GL.End();
            GL.PopMatrix();

            if (isWireframe)
            {
                GL.wireframe = false;
            }
        }

        private void CreateMeshLineMaterial()
        {
            DestroyMeshLineMaterial();

            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixel(0, 0, new Color(0.0f, 0.0f, 1.0f, 0.3f));
            texture.Apply();

            _previewMeshLineMaterial = new Material(Shader.Find("Unlit/Transparent"));
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
