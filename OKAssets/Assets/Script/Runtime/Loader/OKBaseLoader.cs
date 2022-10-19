using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace OKAssets
{
    public abstract class OKBaseLoader : ITicker
    {
        public delegate void OnLoadProgressDelegate(OKBaseLoader loader);

        public delegate void OnLoadCompleteDelegate(OKBaseLoader loader);

        public delegate void OnLoadErrorDelegate(OKBaseLoader loader);

        public OnLoadProgressDelegate OnLoadProgress;
        public OnLoadCompleteDelegate OnLoadComplete;
        public OnLoadErrorDelegate OnLoadError;

        protected string _url;
        
        protected string _name;
        protected float _progress;
        protected ulong _downLoadBytes;
        protected float _startLoadStamp;
        protected float _loadTime = 0;
        protected bool _isLoading = false;
        protected bool _autoDispose = true;
        protected AssetBundle _assetBundle;
        protected bool _isAsync = false;
        private int timeOut = 0;

        private ulong _cacheLoadByte = 0;
        private float _currentTime = 0;
        private float _cacheRate = 10000;

        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }
        

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int TimeOut
        {
            get { return timeOut; }
            set { timeOut = value; }
        }

        public virtual float Progress
        {
            get { return 0; }
        }


        public virtual ulong LoadedBytes
        {
            get { return 0; }
        }

        //加载速率   字节/秒
        public float LoadRate
        {
            get
            {
                if (LoadTime == 0)
                {
                    return 0;
                }

                //下载速度本身就不固定，计算最近0.5秒的平均值
                // if (Time.time - _currentTime >= 1)
                // {
                //     _cacheRate = (LoadedBytes - _cacheLoadByte) / (Time.time - _currentTime);
                //     _cacheLoadByte = LoadedBytes;
                //     _currentTime = Time.time;
                // }

                float rate = LoadedBytes / LoadTime;

                //return _cacheRate == Double.NaN ? 0 : _cacheRate;
                return rate;
            }
        }

        public float LoadTime
        {
            get { return _loadTime; }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
        }

        public bool AutoDispose
        {
            get { return _autoDispose; }
            set { _autoDispose = value; }
        }

        public virtual AssetBundle AssetBundle
        {
            get { return _assetBundle; }
        }

        public bool IsAsync
        {
            get { return _isAsync; }
            set { _isAsync = value; }
        }

        public virtual bool IsFinished
        {
            get { return false; }
        }

        public virtual object Content
        {
            get { return null; }
        }


        public virtual void Load()
        {
            _progress = 0;
            _loadTime = 0;
            _downLoadBytes = 0;
            _startLoadStamp = Time.time;
            _isLoading = true;
        }

        public virtual void Close()
        {
            if (!_isLoading)
            {
                return;
            }

            _isLoading = false;
            _loadTime = Time.time - _startLoadStamp;
        }

        public virtual void Dispose()
        {
            _url = null;
            OnLoadProgress = null;
            OnLoadComplete = null;
            OnLoadError = null;
            _assetBundle = null;
        }

        protected virtual void InvokeLoadComplete()
        {
            if (OnLoadComplete != null)
            {
                OnLoadComplete(this);
            }
        }

        protected virtual void InvokeLoadProgress()
        {
            if (OnLoadProgress != null)
            {
                OnLoadProgress(this);
            }
        }

        protected virtual void InvokeLoadError()
        {
            if (OnLoadError != null)
            {
                OnLoadError(this);
            }
        }

        public abstract void Update();
    }
}