using System;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    // SpriteAssist のジオメトリ生成に失敗したときに投げる。
    // 以前は Unity 既定のジオメトリ（矩形など）へ黙って fallback していたが、import 成果物が
    // 静かに壊れて Accelerator に伝播する原因になるため、明示的に失敗させる。
    public class SpriteAssistGeometryException : Exception
    {
        public SpriteAssistGeometryException(string message) : base(message)
        {
        }
    }

    public static class SpriteUtil
    {
        public static Vector2 GetNormalizedPivot(this Sprite sprite)
        {
            return sprite.pivot / sprite.rect.size;
        }

        public static void GetVertexAndTriangle2D(this Sprite sprite, SpriteConfigData configData, out Vector2[] vertices2D, out ushort[] triangles2D, MeshRenderType meshRenderType, string assetPath = null)
        {
            if (!TryGetVertexAndTriangle2D(sprite, configData, out vertices2D, out triangles2D, meshRenderType, assetPath, out string failureReason))
            {
                throw new SpriteAssistGeometryException(BuildFailureMessage(sprite, configData, meshRenderType, assetPath, failureReason));
            }
        }

        public static void GetVertexAndTriangle3D(this Sprite sprite, SpriteConfigData configData, out Vector3[] vertices3D, out int[] triangles3D, MeshRenderType meshRenderType, string assetPath = null)
        {
            if (!TryGetVertexAndTriangle2D(sprite, configData, out var vertices2D, out var triangles2D, meshRenderType, assetPath, out string failureReason))
            {
                throw new SpriteAssistGeometryException(BuildFailureMessage(sprite, configData, meshRenderType, assetPath, failureReason));
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
            return TryGetVertexAndTriangle2D(sprite, configData, out vertices, out triangles, meshRenderType, assetPath, out _);
        }

        // failureReason: 失敗時の原因（診断ログ用）。成功時は null。
        public static bool TryGetVertexAndTriangle2D(this Sprite sprite, SpriteConfigData configData, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType, string assetPath, out string failureReason)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();
            failureReason = null;

            if (configData == null)
            {
                failureReason = "configData is null";
                return false;
            }

            if (sprite == null)
            {
                failureReason = "source sprite is null (dummy sprite creation may have failed)";
                return false;
            }

            if (sprite.texture == null)
            {
                failureReason = "source sprite has no texture";
                return false;
            }

            bool isUnityDefaultMode = SpriteConfigData.IsUnityDefaultMode(configData.mode);
            string outlineSource;
            if (isUnityDefaultMode && OutlineUtil.TryGetImporterOutline(sprite, assetPath, out var paths))
            {
                outlineSource = "importer outline";
            }
            else
            {
                paths = OutlineUtil.GenerateOutline(sprite, configData, meshRenderType);
                outlineSource = "generated outline";
            }

            int pathCount = paths?.Length ?? 0;
            int pointCount = PathSanitizer.CountPoints(paths);

            if (pointCount == 0)
            {
                failureReason = $"outline is empty ({outlineSource}: paths={pathCount}, points=0, textureSize={sprite.texture.width}x{sprite.texture.height}, readable={sprite.texture.isReadable}). " +
                                "The source texture is probably fully transparent or could not be decoded.";
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
                failureReason = $"too many vertices ({vertices.Length} >= {ushort.MaxValue}; {outlineSource}: paths={pathCount}, points={pointCount})";
                return false;
            }

            // validate empty
            if (vertices.Length <= 0)
            {
                failureReason = $"triangulation produced no vertices ({outlineSource}: paths={pathCount}, points={pointCount}, library={SpriteAssistSettings.ResolvedDefaultTriangulationLibrary})";
                return false;
            }

            return true;
        }

        private static string BuildFailureMessage(Sprite sprite, SpriteConfigData configData, MeshRenderType meshRenderType, string assetPath, string failureReason)
        {
            string path = string.IsNullOrEmpty(assetPath) ? (sprite != null ? AssetDatabase.GetAssetPath(sprite) : "<unknown>") : assetPath;
            string spriteName = sprite != null ? sprite.name : "<null>";
            string mode = configData != null ? configData.mode.ToString() : "<null>";

            return $"[SpriteAssist] Failed to generate geometry. " +
                   $"asset='{path}', sprite='{spriteName}', mode={mode}, renderType={meshRenderType}, " +
                   $"reason: {failureReason} " +
                   $"[graphicsDevice={SystemInfo.graphicsDeviceType}, importWorker={AssetDatabase.IsAssetImportWorkerProcess()}, batchMode={Application.isBatchMode}]";
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

            throw new SpriteAssistGeometryException($"[SpriteAssist] Failed to create dummy sprite: TextureImporter.GetWidthAndHeight is unavailable. Path: {assetPath}");
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
