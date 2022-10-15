using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace OKAssets
{
    public class AssetBundleFromFileLoader : BaseLoader
    {
        private AssetBundleCreateRequest _createRequest;

        public override float Progress
        {
            get
            {
                if (_createRequest == null)
                {
                    return 0;
                }

                return _createRequest.progress;
            }
        }

        public override bool IsFinished
        {
            get
            {
                if (_createRequest == null)
                {
                    return false;
                }

                return _createRequest.isDone;
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

                if (_isAsync)
                {
                    return _createRequest.assetBundle;
                }
                else
                {
                    return _assetBundle;
                }
            }
        }

        public override void Load()
        {
            if (_url == null || _url == string.Empty)
            {
                return;
            }

            _progress = 0;
            _loadTime = 0;
            if (_createRequest != null)
            {
                return;
            }

            if (_isAsync)
            {
                _createRequest = AssetBundle.LoadFromFileAsync(_url);
                if (_createRequest != null)
                {
                    _startLoadStamp = Time.time;
                    _isLoading = true;
                }
            }
            else
            {
                //同步加载
                _assetBundle = AssetBundle.LoadFromFile(_url);
                if (_assetBundle != null)
                {
                    _startLoadStamp = Time.time;
                    _isLoading = true;
                }

                Close();
                InvokeLoadComplete();
                if (_autoDispose)
                {
                    Dispose();
                }
            }
        }

        public override void Close()
        {
            if (!_isLoading)
            {
                return;
            }

            _isLoading = false;
        }

        public override void Update()
        {
            if (_createRequest == null)
            {
                return;
            }

            _loadTime = Time.time - _startLoadStamp;

            if (_createRequest.progress != _progress)
            {
                _progress = _createRequest.progress;
                InvokeLoadProgress();
            }

            if (_createRequest.isDone)
            {
                Close();
                InvokeLoadComplete();
                if (_autoDispose)
                {
                    Dispose();
                }
            }
        }


        protected override void InvokeLoadComplete()
        {
            if (_isAsync)
            {
                _assetBundle = (AssetBundle)Content;
            }

            base.InvokeLoadComplete();
        }

        public override void Dispose()
        {
            _createRequest = null;
            base.Dispose();
        }
    }
}