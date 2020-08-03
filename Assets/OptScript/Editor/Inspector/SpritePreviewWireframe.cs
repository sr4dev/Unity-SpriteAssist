using System.Linq;
using UnityEngine;

namespace OptSprite
{
    public class SpritePreviewWireframe
    {
        public static readonly Color transparentColor = new Color(0.0f, 0.0f, 1.0f, 0.3f);
        public static readonly Color opaqueColor = new Color(1.0f, 0.0f, 0.0f, 0.3f);

        private const string SHADER_NAME = "Unlit/Transparent";
        private static readonly int _mainTex = Shader.PropertyToID("_MainTex");
        private Material _material;
        private Vector2[] _vertices;
        private Vector2[] _scaledVertices;
        private ushort[] _triangles;
        private MeshRenderType _meshRenderType;

        public SpritePreviewWireframe(Color color, MeshRenderType meshRenderType = MeshRenderType.Transparent)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            _material = new Material(Shader.Find(SHADER_NAME));
            _material.SetTexture(_mainTex, texture);
            _material.hideFlags = HideFlags.HideAndDontSave;

            _meshRenderType = meshRenderType;
        }

        public void UpdateAndResize(Rect rect, Sprite sprite, SpriteConfigData data)
        {
            SpriteUtil.GetMeshData(sprite, data, out _vertices, out _triangles, _meshRenderType);

            Resize(rect, sprite);
        }

        public void Resize(Rect rect, Sprite sprite)
        {
            float spriteMinScale = SpriteUtil.GetMinRectScale(rect, sprite.rect);
            _scaledVertices = _vertices.ToArray();
            SpriteUtil.GetScaledVertices(_scaledVertices, sprite.pixelsPerUnit, sprite.pivot, sprite.rect.size, spriteMinScale, true, false);
        }

        public void Draw(Rect rect, Sprite sprite)
        {
            float spriteMinScale = SpriteUtil.GetMinRectScale(rect, sprite.rect);
            Vector2 position = rect.center - (sprite.rect.size * spriteMinScale * 0.5f);

            _material.SetPass(0);

            GLDraw(position, _scaledVertices, _triangles, false);
            GLDraw(position, _scaledVertices, _triangles, true);
        }

        public void Dispose()
        {
            if (_material != null)
            {
                Object.DestroyImmediate(_material.mainTexture);
                Object.DestroyImmediate(_material);
            }
        }

        private void GLDraw(Vector2 pos, Vector2[] vertices, ushort[] triangles, bool isWireframe)
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

    }
}




