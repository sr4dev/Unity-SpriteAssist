using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist.Tests
{
    // -batchmode -nographics 環境で SpriteAssist のジオメトリ生成が正しく動くかを検証する headless エントリポイント。
    // 使い方: unity run <project> -- -nographics -executeMethod SpriteAssist.Tests.HeadlessGeometryCheck.Run
    // 終了コード: 0 = 正常, 1 = 矩形 fallback 検出または import 失敗
    public static class HeadlessGeometryCheck
    {
        private const string TempRoot = "Assets/SpriteAssistHeadlessCheckTemp";
        private const string SourceTexturePath = "Assets/Example/Sprite/rebox-green-tri.png";

        private static readonly SpriteConfigData.Mode[] Modes =
        {
            SpriteConfigData.Mode.TransparentMesh,
            SpriteConfigData.Mode.OpaqueMesh,
            SpriteConfigData.Mode.OpaqueEdgeGridMesh,
            SpriteConfigData.Mode.PixelMesh
        };

        public static void Run()
        {
            int exitCode = 0;

            try
            {
                Debug.Log($"[HeadlessGeometryCheck] graphicsDevice={SystemInfo.graphicsDeviceType}, batchMode={Application.isBatchMode}");

                AssetDatabase.DeleteAsset(TempRoot);
                AssetDatabase.CreateFolder("Assets", TempRoot.Substring("Assets/".Length));

                foreach (SpriteConfigData.Mode mode in Modes)
                {
                    string texturePath = $"{TempRoot}/{mode}.png";
                    if (!AssetDatabase.CopyAsset(SourceTexturePath, texturePath))
                    {
                        throw new InvalidOperationException($"CopyAsset failed: {texturePath}");
                    }

                    var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    importer.userData = JsonUtility.ToJson(new SpriteConfigData { mode = mode, gridSize = 8, gridTolerance = 0.5f });
                    AssetDatabase.WriteImportSettingsIfDirty(texturePath);
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                    if (sprite == null)
                    {
                        Debug.LogError($"[HeadlessGeometryCheck] {mode}: sprite failed to import");
                        exitCode = 1;
                        continue;
                    }

                    bool isRectangle = IsUnityDefaultRectangle(sprite);
                    Debug.Log($"[HeadlessGeometryCheck] {mode}: vertices={sprite.vertices.Length}, triangles={sprite.triangles.Length}, defaultRectangle={isRectangle}");

                    if (isRectangle)
                    {
                        Debug.LogError($"[HeadlessGeometryCheck] {mode}: geometry fell back to Unity default rectangle");
                        exitCode = 1;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                exitCode = 1;
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                Debug.Log($"[HeadlessGeometryCheck] result={(exitCode == 0 ? "OK" : "FAILED")}");
                EditorApplication.Exit(exitCode);
            }
        }

        private static bool IsUnityDefaultRectangle(Sprite sprite)
        {
            Vector2[] vertices = sprite.vertices;
            if (vertices.Length != 4) return false;

            Rect rect = sprite.rect;
            Vector2 pivot = sprite.pivot;
            float ppu = sprite.pixelsPerUnit;
            Vector2[] corners =
            {
                (new Vector2(0, 0) - pivot) / ppu,
                (new Vector2(rect.width, 0) - pivot) / ppu,
                (new Vector2(0, rect.height) - pivot) / ppu,
                (new Vector2(rect.width, rect.height) - pivot) / ppu
            };

            return corners.All(c => vertices.Any(v => (v - c).sqrMagnitude < 1e-6f));
        }
    }
}
