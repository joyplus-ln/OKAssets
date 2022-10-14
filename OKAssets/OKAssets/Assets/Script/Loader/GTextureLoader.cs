using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace OKAssets
{
    public class GTextureLoader : GBinaryLoader
    {
        public Texture2D texture;

        public override void Dispose()
        {
            texture = null;
            base.Dispose();
        }

        protected override DownloadHandler GetDownloadHandler()
        {
            return new DownloadHandlerTexture();
        }

        protected override void InvokeLoadComplete()
        {
            texture = DownloadHandlerTexture.GetContent(_request);
            base.InvokeLoadComplete();
        }
    }
}