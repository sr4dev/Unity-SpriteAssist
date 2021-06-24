using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    [CustomEditor(typeof(SpriteRenderer))]
    [CanEditMultipleObjects]
    public class SpriteRendererInspector : RendererInspectorBase<SpriteRenderer>
    {
        public override Sprite GetRendererSprite()
        {
            return Renderer.sprite;
        }
    }
}