using UnityEngine;
using UnityEngine.Networking;

namespace OKAssets
{
    public class OKAssetBundleLoader : BinaryLoader
    {
        public override AssetBundle AssetBundle
        {
            get
            {
                if (_assetBundle == null)
                    _assetBundle = ((DownloadHandlerAssetBundle)_request.downloadHandler).assetBundle;
                return _assetBundle;
            }
        }

        public override void Dispose()
        {
            _assetBundle = null;
            base.Dispose();
        }

        protected override DownloadHandler GetDownloadHandler()
        {
            return new DownloadHandlerAssetBundle(_url, 0);
        }
        
        
    }
}