using System.Collections.Generic;
using UnityEngine;

namespace OKAssets
{
    public class AtlasInfo
    {
        public string texturePath;
        public Texture2D texture;
        private Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();
        private Sprite[] sprites;

        public void AddSprites(Sprite[] sps)
        {
            for (int i = 0; i < sps.Length; i++)
            {
                Sprite sprite = sps[i];
                string spriteName = sprite.name.Replace("(Clone)", "");
                if (sprite != null)
                {
                    Sprite s = null;
                    if (!spriteDict.TryGetValue(spriteName, out s))
                    {
                        spriteDict.Add(spriteName, sprite);
                    }
                }
            }

            sprites = sps;
        }

        public Dictionary<string, Sprite> GetSpriteMap()
        {
            return spriteDict;
        }

        public Sprite[] GetSprites()
        {
            return sprites;
        }


        public int GetSpriteNum()
        {
            if (sprites == null)
            {
                return 0;
            }

            return sprites.Length;
        }

        public Sprite GetSprite(string name)
        {
            Sprite sprite;
            if (spriteDict.TryGetValue(name, out sprite))
            {
                return sprite;
            }

            return null;
        }
    }
}