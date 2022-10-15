using UnityEngine;
using System.IO;
using UnityEngine.Networking;

namespace OKAssets
{
    public class AssetBundleLoader : BinaryLoader
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

        public bool FileExists(string filepath)
        {
            return true;
        }

        public string ReadFile(string filepath, out string debugpath)
        {
            //Debug.Log(filepath);
            string fileName = filepath.Substring(filepath.LastIndexOf('/') + 1);
            //Debug.Log(fileName);
            string file = null;
            var jsModule = OKResManager.GetInstance().GetJsModuleBundle();
            if (jsModule.ContainsKey(fileName))
            {
                filepath = jsModule[fileName];
            }
#if UNITY_EDITOR
            debugpath = Path.Combine(Application.dataPath.Replace("Assets", "Assets/Res/Scripts/") + filepath);
            file = File.ReadAllText(debugpath);
            debugpath = debugpath.Replace("/", "\\");
            //Debug.Log(debugpath);

#else
                    filepath = "Scripts/" + filepath;
            if (ApplicationKernel.isDebugEnvironment)
        {
            Debug.Log(filepath);
        }
            file = GResManager.GetInstance().LoadTextAsset(filepath + ".txt").text;
            debugpath = Path.Combine(Application.dataPath.Replace("Assets", "TsProj/jsOutPut/") + filepath)
                .Replace("/Scripts", "");
            debugpath = debugpath.Replace("/", "\\");
#endif

            //Debug.Log(debugpath);
            if (file == null)
            {
                Debug.LogError("not find =>" + filepath);
            }

            return file;
        }
    }
}