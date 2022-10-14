using UnityEngine;
using UnityEngine.Networking;

namespace OKAssets
{
    public class GAssetLoader : GBaseLoader
    {
        protected AssetBundleRequest _bundleRequest;
        public AssetBundle assetBundle;
        public string assetName;
        public bool isAsync = true;

        public override float Progress
        {
            get
            {
                if (_bundleRequest == null)
                {
                    return 0;
                }

                return _bundleRequest.progress;
            }
        }

        public override bool IsFinished
        {
            get
            {
                if (_bundleRequest == null)
                {
                    return false;
                }

                return _bundleRequest.isDone;
            }
        }

        public override object Content
        {
            get
            {
                if (!IsFinished)
                {
                    return null;
                }

                return _bundleRequest.asset;
            }
        }

        public override void Load()
        {
            if (assetBundle == null)
            {
                return;
            }

            if (assetName == null || assetName == string.Empty)
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
                _bundleRequest = assetBundle.LoadAssetAsync(assetName);
                if (_bundleRequest != null)
                {
                    _startLoadStamp = Time.time;
                    _isLoading = true;
                }

                TickRunner.GetInstance().AddTicker(this);
            }
        }

        public override void Close()
        {
            if (!_isLoading)
            {
                return;
            }

            _isLoading = false;
            TickRunner.GetInstance().RemoveTicker(this);
        }

        public override void OnTick()
        {
            if (_bundleRequest == null)
            {
                return;
            }

            _loadTime = Time.time - _startLoadStamp;

            if (_bundleRequest.progress != _progress)
            {
                _progress = _bundleRequest.progress;
                InvokeLoadProgress();
            }

            if (_bundleRequest.isDone)
            {
                Close();
                InvokeLoadComplete();
                if (_autoDispose)
                {
                    Dispose();
                }
            }
        }

        public override void OnLateTick()
        {
        }

        public override void Dispose()
        {
            _bundleRequest = null;
            assetBundle = null;
            assetName = null;
            base.Dispose();
        }

        public T GetAsset<T>() where T : UnityEngine.Object
        {
            if (isAsync)
            {
                if (Content == null)
                {
                    return null;
                }

                return Content as T;
            }
            else
            {
                return assetBundle.LoadAsset<T>(assetName);
            }
        }
    }
}