using System;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class SpriteUtil
    {
        public static Vector2 GetNormalizedPivot(this Sprite sprite)
        {
            return sprite.pivot / sprite.rect.size;
        }

        public static void GetVertexAndTriangle2D(this Sprite sprite, SpriteConfigData configData, out Vector2[] vertices2D, out ushort[] triangles2D, MeshRenderType meshRenderType, string assetPath = null)
        {
            if (!TryGetVertexAndTriangle2D(sprite, configData, out vertices2D, out triangles2D, meshRenderType, assetPath))
            {
                //fallback
                vertices2D = sprite.vertices;
                triangles2D = sprite.triangles;
            }
        }

        public static void GetVertexAndTriangle3D(this Sprite sprite, SpriteConfigData configData, out Vector3[] vertices3D, out int[] triangles3D, MeshRenderType meshRenderType, string assetPath = null)
        {
            if (!TryGetVertexAndTriangle2D(sprite, configData, out var vertices2D, out var triangles2D, meshRenderType, assetPath))
            {
                //fallback
                vertices2D = sprite.vertices;
                triangles2D = sprite.triangles;
            }

            vertices3D = vertices2D.ToVector3();
            triangles3D = triangles2D.ToInt();

            if (configData.thickness > 0)
            {
                TriangulationUtil.ExpandMeshThickness(ref vertices3D, ref triangles3D, configData.thickness);
            }
        }

        public static bool TryGetVertexAndTriangle2D(this Sprite sprite, SpriteConfigData configData, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType, string assetPath = null)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();

            if (configData == null || sprite == null)
            {
                return false;
            }

            bool isUnityDefaultMode = SpriteConfigData.IsUnityDefaultMode(configData.mode);
            if (!isUnityDefaultMode || !OutlineUtil.TryGetImporterOutline(sprite, assetPath, out var paths))
            {
                paths = OutlineUtil.GenerateOutline(sprite, configData, meshRenderType);
            }

            if (PathSanitizer.CountPoints(paths) == 0)
            {
                return false;
            }

            if (meshRenderType == MeshRenderType.Grid || meshRenderType == MeshRenderType.TightGrid)
            {
                TriangulationUtil.TriangulateGrid(paths, out vertices, out triangles);
            }
            else
            {
                TriangulationUtil.Triangulate(configData, paths, out vertices, out triangles);
            }

            //validate max
            if (vertices.Length >= ushort.MaxValue)
            {
                Debug.LogErrorFormat($"Too many vertices! Sprite '{sprite.name}' has {vertices.Length} vertices.");
                return false;
            }

            // validate empty
            if (vertices.Length <= 0)
            {
                Debug.LogErrorFormat($"No vertex found! Sprite '{sprite.name}' has something wrong.");
                return false;
            }

            return true;
        }
        
        public static Sprite TryCreateDummySprite(Sprite originalSprite, TextureImporter textureImporter, string assetPath)
        {
            if (Application.isPlaying)
            {
                return originalSprite;
            }

            if (textureImporter.TryGetRawImageSize(out int rawWidth, out int rawHeight))
            {
                string name = originalSprite.name;
                float pixelsPerUnit = originalSprite.pixelsPerUnit;
                int originalWidth = Mathf.RoundToInt(originalSprite.rect.size.x);
                int originalHeight = Mathf.RoundToInt(originalSprite.rect.size.y);
                Vector2 pivot = originalSprite.GetNormalizedPivot();
                Rect rect = new Rect(0, 0, originalWidth, originalHeight);
                Texture2D rawTexture = TextureUtil.GetRawTexture(assetPath, name, originalWidth, originalHeight, rawWidth, rawHeight);
                Sprite newSprite = Sprite.Create(rawTexture, rect, pivot, pixelsPerUnit);
                newSprite.name = name + "(Dummy Sprite)";
                return newSprite;
            }

            Debug.LogError("Fail to create dummy Sprite. Path: " + assetPath);
            return null;
        }

        public static Sprite FindSprite(UnityEngine.Object target)
        {
            switch (target)
            {
                case Sprite s:
                    return s;

                case GameObject go:
                    if (go.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                    {
                        return spriteRenderer.sprite;
                    }
                    
                    if (go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    {
                        if (meshRenderer.sharedMaterial != null)
                        {
                            var mainTexture = meshRenderer.sharedMaterial.GetMainTexture();
                            if (mainTexture != null)
                            {
                                var path = AssetDatabase.GetAssetPath(mainTexture);
                                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                            }
                        }
                    }
                    break;
            }

            return null;
        }

    }

}
