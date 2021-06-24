using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    [CustomEditor(typeof(MeshRenderer))]
    [CanEditMultipleObjects]
    public class MeshRendererInspector : RendererInspectorBase<MeshRenderer>
    {
        public override Sprite GetRendererSprite()
        {
            if (Renderer.sharedMaterial == null)
            {
                return null;
            }

            var path = AssetDatabase.GetAssetPath(Renderer.sharedMaterial.mainTexture);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}