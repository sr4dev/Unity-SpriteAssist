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

        public bool disableBaseGUI = false;

        public Sprite _oldSprite;
        private Sprite _dummySprite;
        private TextureInfo _textureInfo;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            SetSpriteProcessor(target, AssetDatabase.GetAssetPath(target));
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SpriteProcessor?.Dispose();
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }
        
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scroll.scrollPosition;

                    SpriteProcessor?.OnInspectorGUI(disableBaseGUI);

                    if (!disableBaseGUI)
                    {
                        base.OnInspectorGUI();
                    }
                }
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

            bool isTargetChanged = _oldSprite != sprite;
            _oldSprite = sprite;
            
            if (Selection.objects.Length <= SpriteAssistSettings.Settings.maxThumbnailPreviewCount)
            {
                if (isTargetChanged)
                {
                    string assetPath = AssetDatabase.GetAssetPath(sprite);
                    _dummySprite = SpriteUtil.TryCreateDummySprite(sprite, SpriteProcessor.TextureImporter, assetPath);
                    _textureInfo = new TextureInfo(_dummySprite, assetPath);
                }

                SpriteProcessor?.OnPreviewGUI(rect, sprite, _dummySprite, _textureInfo);
            }
        }

        public void SetSpriteProcessor(Object t, string assetPath)
        {
            if (SpriteProcessor == null && t is Sprite sprite && !string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets"))
            {
                SpriteProcessor = new SpriteProcessor(sprite, assetPath);
            }
        }

        private void OnAfterAssemblyReload()
        {
            _oldSprite = null;
        }
    }
}