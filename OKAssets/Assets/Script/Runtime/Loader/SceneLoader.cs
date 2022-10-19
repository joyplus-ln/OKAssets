using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace OKAssets
{
    public class SceneLoader : OKBaseLoader
    {
        private string _sceneName = "";
        private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;
        private AsyncOperation _operation;

        public override float Progress
        {
            get
            {
                if (_operation == null)
                {
                    return 0;
                }

                return _operation.progress;
            }
        }

        public override bool IsFinished
        {
            get
            {
                if (_operation == null)
                {
                    return false;
                }

                return _operation.isDone;
            }
        }

        public string SceneName
        {
            get { return _sceneName; }

            set { _sceneName = value; }
        }

        public LoadSceneMode LoadSceneMode
        {
            get { return _loadSceneMode; }

            set { _loadSceneMode = value; }
        }

        public override void Close()
        {
            if (!_isLoading)
            {
                return;
            }

            _isLoading = false;
            
        }

        public override void Dispose()
        {
            _sceneName = "";
            _operation = null;
            Close();
            base.Dispose();
        }

        public override void Load()
        {
            if (_sceneName == null || _sceneName == string.Empty)
            {
                return;
            }

            _progress = 0;
            _loadTime = 0;
            _operation = SceneManager.LoadSceneAsync(_sceneName, _loadSceneMode);
            if (_operation != null)
            {
                _startLoadStamp = Time.time;
                _isLoading = true;
                
            }
        }

        public override void Update()
        {
            if (_operation == null)
            {
                return;
            }

            _loadTime = Time.time - _startLoadStamp;

            if (_operation.progress != _progress)
            {
                _progress = _operation.progress;
                InvokeLoadProgress();
            }

            if (_operation.isDone)
            {
                Close();
                InvokeLoadComplete();
                if (_autoDispose)
                {
                    Dispose();
                }
            }
        }

    }
}