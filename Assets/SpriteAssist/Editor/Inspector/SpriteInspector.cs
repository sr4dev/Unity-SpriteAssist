using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class SpriteInspector : UnityInternalEditor<Sprite>
    {
        public static bool isSpriteReloaded;
        public bool disableBaseGUI = false;

        private Sprite _oldSprite;
        private Sprite _dummySprite;

        private TextureInfo _textureInfo;
        private Vector2 _scrollPosition;

        public SpriteProcessor SpriteProcessor { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(SpriteAssistSettings.instance.ShouldProcessSprite(target as Sprite))
            {
                SetSpriteProcessor(target, AssetDatabase.GetAssetPath(target));
                AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if(SpriteProcessor != null)
            {
                SpriteProcessor?.Dispose();
                AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            }
        }
        
        public override void OnInspectorGUI()
        {
            if(SpriteProcessor == null)
            {
                base.OnInspectorGUI();
                return;
            }

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
            if (SpriteProcessor == null || sprite == null)
            {
                return;
            }

            if (Selection.objects.Length <= SpriteAssistSettings.instance.maxThumbnailPreviewCount)
            {
                if (_oldSprite != sprite || isSpriteReloaded)
                {
                    string assetPath = AssetDatabase.GetAssetPath(sprite);
                    _dummySprite = SpriteUtil.TryCreateDummySprite(sprite, SpriteProcessor.TextureImporter, assetPath);
                    _textureInfo = new TextureInfo(_dummySprite, assetPath);
                }
                
                if (SpriteProcessor != null && SpriteProcessor.OnPreviewGUI(rect, sprite, _dummySprite, _textureInfo, isSpriteReloaded))
                {
                    isSpriteReloaded = false;
                }
            }

            _oldSprite = sprite;
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