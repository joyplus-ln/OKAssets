using UnityEngine;
using UnityEngine.Networking;

namespace OKAssets
{
    public class TextLoader : BinaryLoader
    {
        private string _text;

        public string Text
        {
            get { return _text; }
        }

        protected override DownloadHandler GetDownloadHandler()
        {
            return new DownloadHandlerBuffer();
        }

        protected override void InvokeLoadComplete()
        {
            _text = _request.downloadHandler.text;
            base.InvokeLoadComplete();
        }

        public override void Dispose()
        {
            _text = null;
            base.Dispose();
        }
    }
}