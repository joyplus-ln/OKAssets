using UnityEngine;

namespace OKAssets
{
    public class OKAssetsConfig : ScriptableObject
    {
        public ResLoadMode loadModel;
        public string appName;
        public string ResFolderName;
        public GameMode gameMode;

        public string CDN_DEBUGFOLDER = "debug";
        public string CDN_RELEASEFOLDER = "release";
    }
}