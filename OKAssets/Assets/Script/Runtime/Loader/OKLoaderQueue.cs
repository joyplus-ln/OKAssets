using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace OKAssets
{
    public class OKLoaderQueue
    {
        public int maxConnection = 6;
        protected Queue<OKBaseLoader> _queue;
        protected List<OKBaseLoader> _loadingList;
        protected int _currentLoadedCount;
        protected int _currentBatchLoaderCount;
        protected int _currentBatchLoadCompleteCount;
        protected int _totalLoadCount;
        private bool _isLoading = false;
        private float _loadRate = 0f;
        private float _progress = 0f;
        private float _progressByteSize = 0f;
        private ulong _finlishedLoadByteSize = 0;
        protected bool _autoDispose = true;

        public delegate void OnLoadCompleteDelegate(OKLoaderQueue queue);

        public delegate void OnLoadProgressDelegate(OKLoaderQueue queue);

        public delegate void OnLoadErrorDelegate(OKLoaderQueue queue);

        public OnLoadCompleteDelegate OnLoadComplete;
        public OnLoadProgressDelegate OnLoadProgress;
        public OnLoadErrorDelegate OnLoadError;

        public OKLoaderQueue()
        {
            _queue = new Queue<OKBaseLoader>();
            _loadingList = new List<OKBaseLoader>();
            _currentLoadedCount = 0;
            _currentBatchLoaderCount = 0;
            _currentBatchLoadCompleteCount = 0;
        }

        public float LoadRate
        {
            get { return _loadRate; }
        }

        public float Progress
        {
            get { return _progress; }
        }

        public float ProgressByteSize
        {
            get { return _progressByteSize; }
        }

        public int CurrentLoadedCount
        {
            get { return _currentLoadedCount; }
        }

        public int TotalLoadCount
        {
            get { return _totalLoadCount; }
        }

        public void AddLoader(OKBaseLoader loader)
        {
            _queue.Enqueue(loader);
            _totalLoadCount = _queue.Count;
        }

        public void AddLoaderAndLoad(OKBaseLoader loader)
        {
            AddLoader(loader);
            Load();
        }

        public void Load()
        {
            if (_isLoading)
            {
                return;
            }

            if (_queue == null || _queue.Count == 0)
            {
                return;
            }

            int nextLoaderCount = Mathf.Min(_queue.Count, maxConnection);
            _currentBatchLoaderCount = nextLoaderCount;
            _currentBatchLoadCompleteCount = 0;

            while (nextLoaderCount > 0)
            {
                OKBaseLoader loader = _queue.Dequeue();
                _loadingList.Add(loader);

                LoadItem(loader);
                --nextLoaderCount;
            }
        }

        private void LoadItem(OKBaseLoader loader)
        {
            if (loader == null)
            {
                return;
            }

            if (!_isLoading)
            {
                _isLoading = true;
            }

            loader.OnLoadComplete += OnLoadItemComplete;
            loader.OnLoadError += OnLoadItemError;
            loader.OnLoadProgress += OnLoadItemProgress;
            loader.Load();
        }

        protected void OnLoadItemComplete(OKBaseLoader loader)
        {
            _loadingList.Remove(loader);
            ++_currentBatchLoadCompleteCount;
            ++_currentLoadedCount;
            _finlishedLoadByteSize += loader.LoadedBytes;
            ExecuteProgressHandler();
            CheckCurrentBatchStatus();
        }

        protected void OnLoadItemError(OKBaseLoader loader)
        {
            _loadingList.Remove(loader);
            ++_currentBatchLoadCompleteCount;
            ExecuteErrorHandler();
            CheckCurrentBatchStatus();
        }

        protected void OnLoadItemProgress(OKBaseLoader loader)
        {
            ExecuteProgressHandler();
        }

        protected void CheckCurrentBatchStatus()
        {
            if (_currentBatchLoadCompleteCount >= _currentBatchLoaderCount)
            {
                _isLoading = false;
                _currentBatchLoaderCount = 0;
                _currentBatchLoadCompleteCount = 0;

                if (_queue.Count > 0)
                {
                    Load();
                }
                else
                {
                    ExecuteCompleteHandler();
                    if (_autoDispose)
                    {
                        Dispose();
                    }
                }
            }
        }

        protected void ExecuteCompleteHandler()
        {
            if (OnLoadComplete != null)
            {
                OnLoadComplete(this);
            }
        }

        protected void ExecuteErrorHandler()
        {
            if (OnLoadError != null)
            {
                OnLoadError(this);
            }
        }

        protected void ExecuteProgressHandler()
        {
            float loadedProgress = 0f;
            float totalRate = 0f;
            float totalRateCount = 0f;
            _progressByteSize = _finlishedLoadByteSize;
            if (_loadingList != null)
            {
                for (int i = 0; i < _loadingList.Count; i++)
                {
                    OKBaseLoader loader = _loadingList[i];
                    loadedProgress += loader.Progress;
                    float loaderRate = loader.LoadRate;
                    if (loaderRate != 0)
                    {
                        totalRate += loaderRate;
                        totalRateCount++;
                    }

                    _progressByteSize += loader.LoadedBytes;
                }

                _loadRate = totalRate / totalRateCount;
                _progress = (loadedProgress + _currentLoadedCount) / _totalLoadCount;
            }

            if (OnLoadProgress != null)
            {
                OnLoadProgress(this);
            }
        }

        public void Dispose()
        {
            _queue = null;
            _loadingList = null;
            OnLoadComplete = null;
            OnLoadError = null;
            OnLoadProgress = null;
        }
    }
}