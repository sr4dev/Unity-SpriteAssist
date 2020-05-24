using System;
using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class OptSpriteInspector : UnitySpriteInspector
    {
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
            DrawSpriteWireframe(rect, sprite);
        }

        private void DrawSpriteWireframe(Rect rect, Sprite sprite)
        {
            float fittingScale = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
            Rect position = new Rect(rect.x, rect.y, sprite.rect.width * fittingScale,
                sprite.rect.height * fittingScale)
            {
                center = rect.center
            };

            ushort[] triangles = sprite.triangles;
            Vector2[] vertices = sprite.vertices;
            float spriteScale = sprite.rect.height / position.height;

            Vector2 pivot = new Vector3(position.x, position.y, 0f);
            //Vector2 spritePivotScaled = sprite.pivot / spriteScale;
            float spritePixelsPerUnitScaled = sprite.pixelsPerUnit / spriteScale;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 vertex = vertices[i] * spritePixelsPerUnitScaled;// + spritePivotScaled;
                vertex.y = vertex.y - position.height;
                vertices[i] = vertex;
            }

            _previewMeshLineMaterial.SetPass(0);
            DrawMesh(pivot, triangles, vertices, false);
            DrawMesh(pivot, triangles, vertices, true);
        }

        private void DrawMesh(Vector2 pivot, ushort[] triangles, Vector2[] vertices, bool isWireframe)
        {
            GL.wireframe = isWireframe;
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(1f, -1f, 1f)));
            GL.Begin(GL.TRIANGLES);

            for (int i = 0; i < triangles.Length; i++)
            {
                GL.Vertex3(vertices[triangles[i]].x, vertices[triangles[i]].y, 0f);
            }

            GL.End();
            GL.PopMatrix();
            GL.wireframe = !isWireframe;
        }

        private void CreateMeshLineMaterial()
        {
            DestroyMeshLineMaterial();

            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixel(0, 0, new Color(0.0f, 0.0f, 1.0f, 0.3f));
            texture.Apply();

            _previewMeshLineMaterial = new Material(Shader.Find("Unlit/Transparent"));
            _previewMeshLineMaterial.SetTexture(Shader.PropertyToID("_MainTex"), texture);
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
