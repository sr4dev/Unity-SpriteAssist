using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist.Tests
{
    public class SpriteImportDataTests
    {
        [Test]
        public void Dispose_DestroysDummySpriteAndTexture()
        {
            const string AssetPath = "Assets/Example/Sprite/cloud.png";
            Sprite sourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath);
            Assert.That(sourceSprite, Is.Not.Null);

            SpriteImportData importData = new SpriteImportData(sourceSprite, AssetPath);
            Sprite dummySprite = importData.dummySprite;
            Texture2D dummyTexture = dummySprite.texture;

            Assert.That(dummySprite, Is.Not.SameAs(sourceSprite));
            Assert.That(dummyTexture, Is.Not.SameAs(sourceSprite.texture));

            importData.Dispose();

            Assert.That(dummySprite == null, Is.True);
            Assert.That(dummyTexture == null, Is.True);
            Assert.That(sourceSprite, Is.Not.Null);
            Assert.That(sourceSprite.texture, Is.Not.Null);
        }
    }
}
