using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    // A duplicated image's .meta inherits the original prefab remap. Mesh Prefab ownership follows the source of its Mesh,
    // so a link to a prefab using another image's Mesh must be redirected to a prefab copied alongside the image or removed.
    public static class MeshPrefabCopyLinker
    {
        // Even during duplication, images and prefabs arrive in separate OnPostprocessAllAssets calls, with images first.
        // Wait until the next editor update and consider prefabs imported in the meantime as copy candidates.
        // shortcut: domain reload loses pending state; affected images retain their links until the next import clears them.
        private static readonly HashSet<string> _pendingTextures = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _importedPrefabs = new HashSet<string>(StringComparer.Ordinal);
        private static bool _scheduled;

        // Collect only while images are pending (images arrive first during duplication). Do nothing for ordinary imports.
        public static void CollectPrefabs(string[] importedAssets)
        {
            if (!_scheduled) return;
            foreach (string path in importedAssets)
            {
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) _importedPrefabs.Add(path);
            }
        }

        // Defer and return true if linked to a prefab using another image's Mesh; do not rename for this image.
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

        // Find the image owning the root Mesh. Ignore legacy embedded Meshes and model Meshes.
        private static string GetMeshOwner(GameObject prefab)
        {
            Mesh mesh = prefab != null && prefab.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
            string path = mesh != null ? AssetDatabase.GetAssetPath(mesh) : null;
            return !string.IsNullOrEmpty(path) && AssetImporter.GetAtPath(path) is TextureImporter ? path : null;
        }

        // The copy is another recently imported prefab using the source image's Mesh. If several match, choose the path closest to the image.
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

        // Point the copied prefab's Mesh and embedded Materials to the new image. External Materials remain shared.
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
