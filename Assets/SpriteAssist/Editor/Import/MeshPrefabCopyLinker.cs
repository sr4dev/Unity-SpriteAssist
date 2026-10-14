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
    public class MeshPrefabCopyLinker : AssetModificationProcessor
    {
        // shortcut: domain reload loses pending copies; add recovery only if script-folder duplication must preserve pairing.
        private static readonly HashSet<string> _pendingTextures = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _createdPaths = new HashSet<string>(StringComparer.Ordinal);
        private static bool _scheduled;

        private static void OnWillCreateAsset(string path)
        {
            if (PrefabUtil.IsAssetImportWorkerProcess()) return;
            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 5);
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) return;
            _createdPaths.Add(path);
            Schedule();
        }

        private static void Schedule()
        {
            if (_scheduled) return;
            _scheduled = true;
            EditorApplication.update += Flush;
        }

        // Defer and return true if linked to a prefab using another image's Mesh; do not rename for this image.
        public static bool Defer(TextureImporter texture, string texturePath)
        {
            if (!IsForeignLink(texture, texturePath, out _, out _)) return false;
            _pendingTextures.Add(texturePath);
            Schedule();
            return true;
        }

        private static void Flush()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;
            _scheduled = false;
            EditorApplication.update -= Flush;
            var textures = _pendingTextures.ToArray();
            var created = new HashSet<string>(_createdPaths, StringComparer.Ordinal);
            var prefabs = created.Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).ToArray();
            _pendingTextures.Clear();
            _createdPaths.Clear();
            // More than one new texture for a source cannot be paired safely by import order.
            var copiesPerOwner = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string path in textures)
            {
                if (!created.Contains(path) || AssetImporter.GetAtPath(path) is not TextureImporter texture ||
                    !IsForeignLink(texture, path, out _, out string owner)) continue;
                copiesPerOwner.TryGetValue(owner, out int count);
                copiesPerOwner[owner] = count + 1;
            }

            foreach (string texturePath in textures)
            {
                if (AssetImporter.GetAtPath(texturePath) is TextureImporter texture && Resolve(texture, texturePath, created, prefabs, copiesPerOwner))
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

        private static bool Resolve(TextureImporter texture, string texturePath, HashSet<string> created, string[] createdPrefabs, Dictionary<string, int> copiesPerOwner)
        {
            if (!IsForeignLink(texture, texturePath, out GameObject linked, out string owner)) return false;
            string linkedPath = AssetDatabase.GetAssetPath(linked);

            GameObject copy = null;
            if (created.Contains(texturePath))
            {
                if (copiesPerOwner.TryGetValue(owner, out int count) && count == 1) copy = FindCopy(linked, owner, texturePath, createdPrefabs);
                else Debug.LogWarning($"[SpriteAssist] Several copied textures use meshes from '{owner}'; none was paired automatically.");
            }
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

        private static GameObject FindCopy(GameObject linked, string owner, string texturePath, string[] createdPrefabs)
        {
            var candidates = createdPrefabs
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(prefab => prefab != null && prefab != linked && GetMeshOwner(prefab) == owner)
                .ToArray();
            if (candidates.Length == 1) return candidates[0];
            if (candidates.Length > 1)
                Debug.LogWarning($"[SpriteAssist] Several copied Mesh Prefabs match '{texturePath}'; none was linked.");
            return null;
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
