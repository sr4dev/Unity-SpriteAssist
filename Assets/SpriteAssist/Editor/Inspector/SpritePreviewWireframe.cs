using System;
using System.Linq;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePreviewWireframe : IDisposable
    {
        public static readonly Color transparentColor = new Color(0.0f, 0.0f, 1.0f, 0.3f);
        public static readonly Color opaqueColor = new Color(1.0f, 0.0f, 0.0f, 0.3f);

        private const string SHADER_NAME = "Unlit/Transparent";
        private static readonly int _mainTex = Shader.PropertyToID("_MainTex");
        private Material _material;
        private Vector2[] _vertices;
        private Vector2[] _scaledVertices;
        private ushort[] _triangles;
        private readonly MeshRenderType _meshRenderType;

        public SpritePreviewWireframe(Color color, MeshRenderType meshRenderType)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            _material = new Material(Shader.Find(SHADER_NAME));
            _material.SetTexture(_mainTex, texture);
            _material.hideFlags = HideFlags.HideAndDontSave;

            _meshRenderType = meshRenderType;
        }

        public void UpdateAndResize(Rect rect, Sprite sprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            sprite.GetVertexAndTriangle2D(data, out _vertices, out _triangles, _meshRenderType);
            Vector2[] vertices = _vertices.ToArray();
            float spriteMinScale = GetMinRectScale(rect, textureInfo.rect);
            _scaledVertices = MeshUtil.GetScaledVertices(vertices, textureInfo, spriteMinScale, true);
        }
        
        public void Draw(Rect rect, TextureInfo textureInfo)
        {
            float spriteMinScale = GetMinRectScale(rect, textureInfo.rect);
            Vector2 position = rect.center - (textureInfo.rect.size * spriteMinScale * 0.5f);

            _material.SetPass(0);

            GLDraw(position, _scaledVertices, _triangles, false);
            GLDraw(position, _scaledVertices, _triangles, true);
        }

        public void Dispose()
        {
            if (_material != null)
            {
                UnityEngine.Object.DestroyImmediate(_material.mainTexture);
                UnityEngine.Object.DestroyImmediate(_material);
            }
        }

        public string GetInfo(TextureInfo textureInfo)
        {
            string icon = "//";
            switch (_meshRenderType)
            {
                case MeshRenderType.Transparent:
                case MeshRenderType.SeparatedTransparent:
                    icon = "<color=blue>" + icon + "</color>";
                    break;

                case MeshRenderType.Opaque:
                    icon = "<color=red>" + icon + "</color>";
                    break;
            }
            
            return icon + " " + MeshUtil.GetAreaInfo(_vertices, _triangles, textureInfo);
        }

        private static void GLDraw(Vector2 pos, Vector2[] vertices, ushort[] triangles, bool isWireframe)
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

        private static float GetMinRectScale(Rect rect, Rect sRect)
        {
            return Mathf.Min(rect.width / sRect.width, rect.height / sRect.height);
        }

    }
}




