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

            var mainTexture = Renderer.sharedMaterial.GetMainTexture();
            var path = AssetDatabase.GetAssetPath(mainTexture);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}