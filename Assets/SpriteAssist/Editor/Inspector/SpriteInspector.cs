using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class SpriteInspector : UnityInternalEditor<Sprite>
    {
        private Vector2 _scrollPosition;

        public SpriteProcessor SpriteProcessor { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetSpriteProcessor(target, AssetDatabase.GetAssetPath(target));
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SpriteProcessor?.Dispose();
        }
        
        public override void OnInspectorGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                SpriteProcessor?.OnInspectorGUI();

                base.OnInspectorGUI();
            }
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            //original preview
            base.OnPreviewGUI(rect, background);

            Sprite sprite = target as Sprite;

            if (sprite == null)
            {
                return;
            }

            if (Selection.objects.Length <= SpriteAssistSettings.Settings.maxThumbnailPreviewCount)
            {
                TextureInfo textureInfo = new TextureInfo(AssetDatabase.GetAssetPath(target), sprite);
                SpriteProcessor?.OnPreviewGUI(rect, sprite, textureInfo);
            }
        }

        public void SetSpriteProcessor(Object t, string assetPath)
        {
            if (SpriteProcessor == null && t is Sprite sprite && !string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets"))
            {
                SpriteProcessor = new SpriteProcessor(sprite, assetPath);
            }
        }
    }
}