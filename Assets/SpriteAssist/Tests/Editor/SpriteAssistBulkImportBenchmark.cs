using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace SpriteAssist.Tests
{
    public class SpriteAssistBulkImportBenchmark
    {
        private const string TempRoot = "Assets/SpriteAssistBenchmarkTemp";
        private const string SourceTexturePath = "Assets/Example/Sprite/dead_tree.png";
        private const string SourcePrefabPath = "Assets/Example/Sprite/cloud.prefab";

        [UnityTest, Timeout(900000)]
        public IEnumerator BulkImport100()
        {
            return Run(100);
        }

        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator LargeBulkImport1000()
        {
            return Run(1000);
        }

        private static IEnumerator Run(int assetCount)
        {
            Assert.That(EditorSettings.refreshImportMode, Is.EqualTo(AssetDatabase.RefreshImportMode.OutOfProcessPerQueue));
            AssetDatabase.DesiredWorkerCount = 2;
            AssetDatabase.ForceToDesiredWorkerCount();

            try
            {
                PrepareFixtures(assetCount);
                EditorUtility.UnloadUnusedAssetsImmediate();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                BenchmarkResult result = new BenchmarkResult
                {
                    assetCount = assetCount,
                    refreshImportMode = EditorSettings.refreshImportMode.ToString(),
                    desiredWorkerCount = AssetDatabase.DesiredWorkerCount,
                    processId = Process.GetCurrentProcess().Id,
                    reservedMemoryBefore = Profiler.GetTotalReservedMemoryLong(),
                    allocatedMemoryBefore = Profiler.GetTotalAllocatedMemoryLong(),
                    textureCountBefore = Resources.FindObjectsOfTypeAll<Texture2D>().Length,
                    spriteCountBefore = Resources.FindObjectsOfTypeAll<Sprite>().Length,
                    meshCountBefore = Resources.FindObjectsOfTypeAll<Mesh>().Length
                };

                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < assetCount; i++)
                {
                    // OS から検出される一括変更にして Parallel Import の経路を通す。
                    using FileStream stream = new FileStream(GetTexturePath(i), FileMode.Append, FileAccess.Write, FileShare.Read);
                    stream.WriteByte((byte)(i % byte.MaxValue));
                }

                long[] prefabWriteTimes = new long[assetCount];
                for (int i = 0; i < assetCount; i++)
                {
                    prefabWriteTimes[i] = File.GetLastWriteTimeUtc(GetPrefabPath(i)).Ticks;
                }

                AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);
                result.refreshElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                // Mesh はテクスチャ import の成果物なので、import 後の prefab flush は存在しない。
                yield return null;

                stopwatch.Stop();
                result.elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.flushElapsedMilliseconds = result.elapsedMilliseconds - result.refreshElapsedMilliseconds;

                for (int i = 0; i < assetCount; i++)
                {
                    if (File.GetLastWriteTimeUtc(GetPrefabPath(i)).Ticks != prefabWriteTimes[i])
                    {
                        result.prefabWriteCount++;
                    }
                }

                Assert.That(result.prefabWriteCount, Is.Zero);

                stopwatch.Restart();
                for (int i = 0; i < assetCount; i++)
                {
                    using FileStream stream = new FileStream(GetTexturePath(i), FileMode.Append, FileAccess.Write, FileShare.Read);
                    stream.WriteByte((byte)((i + 1) % byte.MaxValue));
                }

                AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);
                result.noOpRefreshElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                yield return null;

                stopwatch.Stop();
                result.noOpElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.noOpFlushElapsedMilliseconds = result.noOpElapsedMilliseconds - result.noOpRefreshElapsedMilliseconds;
                for (int i = 0; i < assetCount; i++)
                {
                    if (File.GetLastWriteTimeUtc(GetPrefabPath(i)).Ticks != prefabWriteTimes[i])
                    {
                        result.noOpPrefabWriteCount++;
                    }
                }

                Assert.That(result.noOpPrefabWriteCount, Is.Zero);

                VerifyFixtureGeometry();
                EditorUtility.UnloadUnusedAssetsImmediate();

                result.reservedMemoryAfter = Profiler.GetTotalReservedMemoryLong();
                result.allocatedMemoryAfter = Profiler.GetTotalAllocatedMemoryLong();
                result.textureCountAfter = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
                result.spriteCountAfter = Resources.FindObjectsOfTypeAll<Sprite>().Length;
                result.meshCountAfter = Resources.FindObjectsOfTypeAll<Mesh>().Length;

                Directory.CreateDirectory("Logs");
                string outputPath = $"Logs/SpriteAssistBulkImportBenchmark-{assetCount}.json";
                File.WriteAllText(outputPath, JsonUtility.ToJson(result, true));
                TestContext.Progress.WriteLine($"SpriteAssist bulk import benchmark: {outputPath}");
                TestContext.Progress.WriteLine(JsonUtility.ToJson(result));
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempRoot);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        private static void PrepareFixtures(int assetCount)
        {
            AssetDatabase.DeleteAsset(TempRoot);
            Assert.That(AssetDatabase.CreateFolder("Assets", Path.GetFileName(TempRoot)), Is.Not.Empty);

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < assetCount; i++)
                {
                    Assert.That(AssetDatabase.CopyAsset(SourceTexturePath, GetTexturePath(i)), Is.True);
                    Assert.That(AssetDatabase.CopyAsset(SourcePrefabPath, GetPrefabPath(i)), Is.True);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            for (int i = 0; i < assetCount; i++)
            {
                string texturePath = GetTexturePath(i);
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(i));

                Assert.That(importer, Is.Not.Null);
                Assert.That(prefab, Is.Not.Null);

                var identifier = new AssetImporter.SourceAssetIdentifier(
                    typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER);
                importer.AddRemap(identifier, prefab);
                Assert.That(AssetDatabase.WriteImportSettingsIfDirty(texturePath), Is.True);
            }
        }

        private static string GetTexturePath(int index)
        {
            return $"{TempRoot}/Bulk_{index:D4}.png";
        }

        private static string GetPrefabPath(int index)
        {
            return $"{TempRoot}/Bulk_{index:D4}.prefab";
        }

        private static void VerifyFixtureGeometry()
        {
            string texturePath = GetTexturePath(0);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);

            Assert.That(SpriteMeshAssets.TryGetMeshes(texturePath, out Mesh mesh, out _), Is.True);
            Assert.That(mesh.vertices, Is.EqualTo(sprite.vertices.ToVector3()));
            Assert.That(mesh.triangles, Is.EqualTo(sprite.triangles.ToInt()));
        }

        [Serializable]
        private class BenchmarkResult
        {
            public int assetCount;
            public string refreshImportMode;
            public int desiredWorkerCount;
            public int processId;
            public long elapsedMilliseconds;
            public long refreshElapsedMilliseconds;
            public long flushElapsedMilliseconds;
            public long noOpElapsedMilliseconds;
            public long noOpRefreshElapsedMilliseconds;
            public long noOpFlushElapsedMilliseconds;
            public int prefabWriteCount;
            public int noOpPrefabWriteCount;
            public long reservedMemoryBefore;
            public long reservedMemoryAfter;
            public long allocatedMemoryBefore;
            public long allocatedMemoryAfter;
            public int textureCountBefore;
            public int textureCountAfter;
            public int spriteCountBefore;
            public int spriteCountAfter;
            public int meshCountBefore;
            public int meshCountAfter;
        }
    }
}
