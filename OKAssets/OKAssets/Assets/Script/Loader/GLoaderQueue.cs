using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace OKAssets
{
    public class GLoaderQueue
    {
        public int maxConnection = 6;
        protected Queue<GBaseLoader> _queue;
        protected List<GBaseLoader> _loadingList;
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

        public delegate void OnLoadCompleteDelegate(GLoaderQueue queue);

        public delegate void OnLoadProgressDelegate(GLoaderQueue queue);

        public delegate void OnLoadErrorDelegate(GLoaderQueue queue);

        public OnLoadCompleteDelegate OnLoadComplete;
        public OnLoadProgressDelegate OnLoadProgress;
        public OnLoadErrorDelegate OnLoadError;

        public GLoaderQueue()
        {
            _queue = new Queue<GBaseLoader>();
            _loadingList = new List<GBaseLoader>();
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

        public void AddLoader(GBaseLoader loader)
        {
            _queue.Enqueue(loader);
            _totalLoadCount = _queue.Count;
        }

        public void AddLoaderAndLoad(GBaseLoader loader)
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
                GBaseLoader loader = _queue.Dequeue();
                _loadingList.Add(loader);

                LoadItem(loader);
                --nextLoaderCount;
            }
        }

        private void LoadItem(GBaseLoader loader)
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

        protected void OnLoadItemComplete(GBaseLoader loader)
        {
            _loadingList.Remove(loader);
            ++_currentBatchLoadCompleteCount;
            ++_currentLoadedCount;
            _finlishedLoadByteSize += loader.LoadedBytes;
            ExecuteProgressHandler();
            CheckCurrentBatchStatus();
        }

        protected void OnLoadItemError(GBaseLoader loader)
        {
            _loadingList.Remove(loader);
            ++_currentBatchLoadCompleteCount;
            ExecuteErrorHandler();
            CheckCurrentBatchStatus();
        }

        protected void OnLoadItemProgress(GBaseLoader loader)
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
                    GBaseLoader loader = _loadingList[i];
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