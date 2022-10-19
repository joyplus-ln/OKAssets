﻿using System;

namespace OKAssets
{
    [Serializable]
    public class TextureAssets
    {
        public TextureAsset[] assets;

        public TextureAssets(TextureAsset[] assets)
        {
            this.assets = assets;
        }
    }

    [Serializable]
    public class TextureAsset
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public string name;
    }
}