using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace OKAssets
{
    public class BinaryLoader : BaseLoader
    {
        protected UnityWebRequest _request;

        public override float Progress
        {
            get
            {
                if (_request == null)
                {
                    return 0;
                }

                return _request.downloadProgress;
            }
        }

        public override ulong LoadedBytes
        {
            get { return _downLoadBytes; }
        }

        public bool IsHttpError
        {
            get
            {
                if (_request == null)
                {
                    return false;
                }

                return _request.isHttpError;
            }
        }


        public bool IsNetworkError
        {
            get
            {
                if (_request == null)
                {
                    return false;
                }

                return _request.isNetworkError;
            }
        }

        public string Error
        {
            get
            {
                if (_request == null)
                {
                    return "";
                }

                return _request.error;
            }
        }

        public override bool IsFinished
        {
            get
            {
                if (_request == null)
                {
                    return false;
                }

                if (_request.downloadHandler == null)
                {
                    return false;
                }

                return _request.isDone && _request.downloadHandler.isDone;
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

                return _request.downloadHandler.data;
            }
        }

        protected virtual DownloadHandler GetDownloadHandler()
        {
            return new DownloadHandlerBuffer();
        }

        public virtual void Load(string method, WWWForm form, Dictionary<string, string> header)
        {
            if (_request == null)
            {
                if (method.Equals(UnityWebRequest.kHttpVerbPOST))
                {
                    _request = UnityWebRequest.Post(_url, form);
                }
                else if (method.Equals(UnityWebRequest.kHttpVerbGET))
                {
                    _request = UnityWebRequest.Get(_url);
                }

                if (header != null)
                {
                    foreach (string headerKey in header.Keys)
                    {
                        _request.SetRequestHeader(headerKey, header[headerKey]);
                    }
                }
            }

            if (_request != null)
            {
                Load();
            }
        }

        public virtual void Load(string method, byte[] postBytes, Dictionary<string, string> header)
        {
            if (_request == null)
            {
                if (method.Equals(UnityWebRequest.kHttpVerbPOST))
                {
                    _request = UnityWebRequest.Post(_url, UnityWebRequest.kHttpVerbPOST);
                    _request.uploadHandler = (UploadHandler)new UploadHandlerRaw(postBytes);
                    _request.SetRequestHeader("Content-Type", "application/json");
                }
                else if (method.Equals(UnityWebRequest.kHttpVerbGET))
                {
                    _request = UnityWebRequest.Get(_url);
                }

                if (header != null)
                {
                    foreach (string headerKey in header.Keys)
                    {
                        _request.SetRequestHeader(headerKey, header[headerKey]);
                    }
                }
            }


            if (_request != null)
            {
                Load();
            }
        }

        public override void Load()
        {
            if (_url == null || _url == string.Empty)
            {
                return;
            }

            if (_request == null)
            {
                _request = new UnityWebRequest();
            }

            _request.disposeDownloadHandlerOnDispose = true;
            if (_url.StartsWith("http") == false && _url.StartsWith("https") == false &&
                _url.StartsWith("jar") == false)
            {
                if (_url.StartsWith("file://") == false)
                {
                    _url = "file://" + _url;
                }
            }

            if (_request.url != _url)
            {
                _request.url = _url;
            }

            if (_request.downloadHandler == null)
            {
                _request.downloadHandler = GetDownloadHandler();
            }

            if (_request != null && this.TimeOut != 0)
            {
                _request.timeout = this.TimeOut;
            }

            _request.SendWebRequest();
            base.Load();
        }


        public override void Close()
        {
            if (_request != null)
            {
                _request.Abort();
            }

            base.Close();
        }

        public override void Update()
        {
            if (_request == null)
            {
                return;
            }

            _loadTime = Time.time - _startLoadStamp;
            if (IsError())
            {
                Close();
                InvokeLoadError();
                if (_autoDispose)
                {
                    Dispose();
                }

                return;
            }

            if (_request.downloadProgress != _progress)
            {
                _progress = _request.downloadProgress;
                _downLoadBytes = _request.downloadedBytes;
                InvokeLoadProgress();
            }

            if (_request.isDone && _request.downloadHandler.isDone &&
                (_request.responseCode == 200 || _request.responseCode == 201))
            {
                Close();
                InvokeLoadComplete();
                if (_autoDispose)
                {
                    Dispose();
                }
            }
        }


        public override void Dispose()
        {
            Close();
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }

            base.Dispose();
        }

        protected bool IsError()
        {
            if ((_request.isHttpError || _request.isNetworkError))
            {
                Debug.Log(_url + "-" + "error ");
            }

            return false;
        }
    }
}