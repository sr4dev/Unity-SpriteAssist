using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    // 複製された画像の .meta は元 prefab への remap を引き継ぐ。Mesh Prefab の所有者は Mesh の出所なので、
    // 他画像の Mesh を使う prefab へのリンクは「一緒に複製された prefab へ付け替える」か「解除する」。
    public static class MeshPrefabCopyLinker
    {
        // 複製でも画像と prefab は別々の OnPostprocessAllAssets で届く（画像が先）。
        // 判定は次の editor update まで待ち、その間に import された prefab を複製先の候補にする。
        // shortcut: domain reload で保留中の情報は消える。消えた画像はリンクを保持し、次の import で解除される。
        private static readonly HashSet<string> _pendingTextures = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _importedPrefabs = new HashSet<string>(StringComparer.Ordinal);
        private static bool _scheduled;

        // 保留中の画像がある間だけ集める（複製では画像が先に届く）。通常の import では何もしない。
        public static void CollectPrefabs(string[] importedAssets)
        {
            if (!_scheduled) return;
            foreach (string path in importedAssets)
            {
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) _importedPrefabs.Add(path);
            }
        }

        // 他画像の Mesh を使う prefab にリンクしていれば保留して true（その画像の rename は行わない）
        public static bool Defer(TextureImporter texture, string texturePath)
        {
            if (!IsForeignLink(texture, texturePath, out _, out _)) return false;
            _pendingTextures.Add(texturePath);
            if (!_scheduled)
            {
                _scheduled = true;
                EditorApplication.update += Flush;
            }
            return true;
        }

        private static void Flush()
        {
            _scheduled = false;
            EditorApplication.update -= Flush;
            var textures = _pendingTextures.ToArray();
            var prefabs = _importedPrefabs.ToArray();
            _pendingTextures.Clear();
            _importedPrefabs.Clear();

            foreach (string texturePath in textures)
            {
                if (AssetImporter.GetAtPath(texturePath) is TextureImporter texture && Resolve(texture, texturePath, prefabs))
                    AssetDatabase.WriteImportSettingsIfDirty(texturePath);
            }
        }

        private static bool IsForeignLink(TextureImporter texture, string texturePath, out GameObject linked, out string owner)
        {
            linked = null;
            owner = null;
            if (!SpriteImportData.TryGetMeshPrefabPath(texture, texturePath, out string linkedPath)) return false;
            linked = AssetDatabase.LoadAssetAtPath<GameObject>(linkedPath);
            owner = GetMeshOwner(linked);
            return owner != null && owner != texturePath;
        }

        private static bool Resolve(TextureImporter texture, string texturePath, string[] importedPrefabs)
        {
            if (!IsForeignLink(texture, texturePath, out GameObject linked, out string owner)) return false;
            string linkedPath = AssetDatabase.GetAssetPath(linked);

            GameObject copy = FindCopy(linked, owner, texturePath, importedPrefabs);
            Unlink(texture, texturePath);
            if (copy != null && Retarget(copy, owner, texturePath))
            {
                texture.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER), copy);
                Debug.Log($"[SpriteAssist] '{texturePath}' now links its copied Mesh Prefab '{AssetDatabase.GetAssetPath(copy)}'.");
            }
            else
            {
                Debug.Log($"[SpriteAssist] '{texturePath}' was linked to '{linkedPath}', whose mesh belongs to '{owner}'. The link was removed.");
            }
            return true;
        }

        // root の Mesh を持つ画像。旧構造（埋め込み Mesh）やモデル Mesh は判定しない。
        private static string GetMeshOwner(GameObject prefab)
        {
            Mesh mesh = prefab != null && prefab.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
            string path = mesh != null ? AssetDatabase.GetAssetPath(mesh) : null;
            return !string.IsNullOrEmpty(path) && AssetImporter.GetAtPath(path) is TextureImporter ? path : null;
        }

        // 直近に import された、元画像の Mesh を使う別の prefab が複製先。複数あれば画像に最も近いパスを選ぶ。
        private static GameObject FindCopy(GameObject linked, string owner, string texturePath, string[] importedPrefabs)
        {
            var candidates = importedPrefabs
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(prefab => prefab != null && prefab != linked && GetMeshOwner(prefab) == owner)
                .GroupBy(prefab => CommonPrefixLength(AssetDatabase.GetAssetPath(prefab), texturePath))
                .OrderByDescending(group => group.Key)
                .FirstOrDefault();
            if (candidates == null) return null;
            if (candidates.Count() == 1) return candidates.First();
            Debug.LogWarning($"[SpriteAssist] Several copied Mesh Prefabs match '{texturePath}'; none was linked.");
            return null;
        }

        private static int CommonPrefixLength(string a, string b)
        {
            int i = 0;
            while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
            return i;
        }

        // 複製先 prefab の Mesh と埋め込み Material を新しい画像へ向ける。外部 Material は共有のまま。
        private static bool Retarget(GameObject prefab, string sourcePath, string destinationPath)
        {
            if (!SpriteMeshAssets.TryGetMeshes(sourcePath, out Mesh sourceRoot, out Mesh sourceSub) ||
                !SpriteMeshAssets.TryGetMeshes(destinationPath, out Mesh destinationRoot, out Mesh destinationSub)) return false;

            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Any(f => f.sharedMesh != null && f.sharedMesh == sourceSub && destinationSub == null))
            {
                Debug.LogWarning($"[SpriteAssist] '{destinationPath}' has no sub mesh for '{AssetDatabase.GetAssetPath(prefab)}' (mesh modes differ).");
                return false;
            }

            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh == null) continue;
                if (filter.sharedMesh == sourceRoot) filter.sharedMesh = destinationRoot;
                else if (filter.sharedMesh == sourceSub) filter.sharedMesh = destinationSub;
            }

            var sourceTexture = AssetDatabase.LoadAssetAtPath<Texture>(sourcePath);
            var destinationTexture = AssetDatabase.LoadAssetAtPath<Texture>(destinationPath);
            foreach (Material material in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(prefab)).OfType<Material>())
            {
                foreach (string property in material.GetTexturePropertyNames())
                {
                    if (material.GetTexture(property) != sourceTexture) continue;
                    material.SetTexture(property, destinationTexture);
                    EditorUtility.SetDirty(material);
                }
            }

            PrefabUtility.SavePrefabAsset(prefab);
            return true;
        }

        private static void Unlink(TextureImporter texture, string texturePath)
        {
            texture.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), Path.GetFileNameWithoutExtension(texturePath)));
            texture.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(GameObject), SpriteImportData.MESH_PREFAB_IDENTIFIER));
        }
    }
}
