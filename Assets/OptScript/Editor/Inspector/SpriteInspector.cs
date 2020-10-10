using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class SpriteInspector : UnityInternalEditor<Sprite>
    {
        private SpriteProcessor _spriteProcessor;

        protected override void OnEnable()
        {
            base.OnEnable();

            Sprite sprite = target as Sprite;
            string assetPath = AssetDatabase.GetAssetPath(target);

            if (sprite == null || string.IsNullOrEmpty(assetPath))
            {
                //reimport 
                return;
            }

            _spriteProcessor = new SpriteProcessor(sprite, assetPath);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            _spriteProcessor?.Dispose();
        }

        public override void OnInspectorGUI()
        {
            _spriteProcessor?.OnInspectorGUI(target, targets);

            base.OnInspectorGUI();
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            //original preview
            base.OnPreviewGUI(rect, background);

            _spriteProcessor?.OnPreviewGUI(rect, target);

        }
    }
}