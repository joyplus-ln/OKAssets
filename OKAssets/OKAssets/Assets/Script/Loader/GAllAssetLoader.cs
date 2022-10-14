using UnityEngine;
using System.Collections;

namespace OKAssets
{
    public class GAllAssetLoader : GAssetLoader
    {
        public override object Content
        {
            get { return null; }
        }

        public override void Load()
        {
            if (assetBundle == null)
            {
                return;
            }

            _progress = 0;
            _loadTime = 0;
            if (_bundleRequest != null)
            {
                return;
            }

            if (isAsync)
            {
                _bundleRequest = assetBundle.LoadAllAssetsAsync();
                if (_bundleRequest != null)
                {
                    _startLoadStamp = Time.time;
                    _isLoading = true;
                }

                TickRunner.GetInstance().AddTicker(this);
            }
        }


        public T[] GetAllAssets<T>() where T : UnityEngine.Object
        {
            if (isAsync)
            {
                if (!IsFinished)
                {
                    return null;
                }

                return _bundleRequest.allAssets as T[];
            }
            else
            {
                return assetBundle.LoadAllAssets<T>();
            }
        }
    }
}