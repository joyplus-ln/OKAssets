using UnityEngine;

namespace OKAssets
{
    public class OKAssetsConfig : ScriptableObject
    {
        /// <summary>
        /// 资源加载模式，编辑器加载 在线加载模式
        /// </summary>
        public ResLoadMode loadModel = ResLoadMode.EditorModel;
        public string appName = "OKAssetsDemo";
        public string ResFolderName = "Res";
        public GameMode gameMode = GameMode.DEBUG;
        public string CDN_DEBUGFOLDER = "debug";
        public string CDN_RELEASEFOLDER = "release";
    }
}