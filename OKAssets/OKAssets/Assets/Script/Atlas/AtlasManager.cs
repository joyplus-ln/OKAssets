using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OKAssets;
using UnityEngine;
using UnityEngine.U2D;

namespace OKAssets
{
    public class AtlasManager
    {
        public delegate void OnGetAtlasComplete(AtlasInfo atlas);

        public delegate string GetAtlasPath(string AtlasName);

        private Dictionary<string, AtlasInfo> atlasDict;

        private static AtlasManager _instance;

        public static AtlasManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AtlasManager();
            }

            return _instance;
        }

        public void Init(GetAtlasPath callBack)
        {
            atlasDict = new Dictionary<string, AtlasInfo>();
            SpriteAtlasManager.atlasRequested += (string tag, System.Action<SpriteAtlas> action) =>
            {
                //Debug.LogError("tag:" + tag); //tag是SpriteAtlas资源的文件名称
                // GResManager.GetInstance()
                //     .LoadAssetAsync(callBack(tag), (asset) => { action(asset as SpriteAtlas); });
                action(OKResManager.GetInstance().LoadAsset(callBack(tag)) as SpriteAtlas);
            };
        }

        public void AddAtlas(string texturePath, AtlasInfo atlasInfo)
        {
            AtlasInfo atlas;
            if (!atlasDict.TryGetValue(texturePath, out atlas))
            {
                atlasDict.Add(texturePath, atlasInfo);
            }
        }

        public AtlasInfo GetAtlas(string texturePath)
        {
            AtlasInfo atlas;
            if (atlasDict.TryGetValue(texturePath, out atlas))
            {
                return atlas;
            }

            return null;
        }

        public AtlasInfo LoadAtlas(string atlasFullPath)
        {
            AtlasInfo atlas = GetAtlas(atlasFullPath);
            if (atlas != null)
            {
                return atlas;
            }

            return OKResManager.GetInstance().LoadAllSpriteInAtlas(atlasFullPath);
        }

        public void LoadAtlasAsync(string texturePath, OnGetAtlasComplete callback = null)
        {
            AtlasInfo atlas = GetAtlas(texturePath);
            if (atlas != null)
            {
                if (callback != null)
                {
                    callback(atlas);
                }

                return;
            }

            OKResManager.GetInstance().LoadAllSpriteInAtlasAsync(texturePath, delegate(AtlasInfo atlasInfo)
            {
                if (callback != null)
                {
                    callback(atlasInfo);
                }
            });
        }

        public void RemoveAtlas(string texturePath)
        {
            AtlasInfo atlas;
            if (atlasDict.TryGetValue(texturePath, out atlas))
            {
                atlasDict.Remove(texturePath);
            }
        }

        public AtlasInfo LoadTextureWithAtlasInfo(string texturePath)
        {
            AtlasInfo atlas = GetAtlas(texturePath);
            if (atlas != null)
            {
                return atlas;
            }

            Texture2D tex = OKResManager.GetInstance().LoadTexture2D(texturePath);
            atlas = new AtlasInfo();
            atlas.texturePath = texturePath;
            atlas.texture = tex;
            Sprite[] sps = new Sprite[1];
            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            sps[0] = s;
            atlas.AddSprites(sps);
            AddAtlas(texturePath, atlas);
            return atlas;
        }
    }

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