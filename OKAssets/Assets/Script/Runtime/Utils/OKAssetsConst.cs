using UnityEngine;

namespace OKAssets
{
    public class OKAssetsConst
    {
        public static OKAssetsConfig OkConfig
        {
            get
            {
                return Resources.Load<OKAssetsConfig>("OKAssetsConfig");
            }
        }
        public const string CONFIGNAME = "OKAssetsConfig.asset";
        public const string ASSETBUNDLE_FOLDER = "AssetBundles";
        public const string VARIANT = ".ab";
        public const string ASSET_PATH_PREFIX = "Assets/Bundles/";
        public const string OKAssetBundleData = "Assets/OKAssetBundleData.asset";
        public const string OKAssetBundleTagData = "OKAssetBundleTagData.asset";
        public const string OKAssetBundleVersionData = "OKAssetBundleVersionData.asset";
        public const string ATLASTAG = "Atlas";
        public const string BUNDLEFOLDER = "Bundles";
        
        public const string FILENAME_FILES_TXT = "bundleFiles.txt";
        public const string FILENAME_BUILDVERSION_TXT = "buildversion.txt";
        public const string Basic = "basic";

    }

    public enum ResLoadMode
    {
        EditorModel,
        OnLineModel,
    }
    
    
    public enum BundleStorageLocation
    {
        NONE = 0,
        STREAMINGASSETS = 1, //存放在包体的StreamingAssets文件夹里，Application.streamingAssetsPath
        STORAGE = 2, //存放在本地存储中，Application.persistentDataPath
        CDN = 3, //保存在CDN上的
    }

    public enum BundleLocation
    {
        Local = 0, //保存在本地的
        OnLine = 1, //保存在线上的
    }


    struct GetNewVersion //用于通过POST请求线上的版本号
    {
        public string AppVersion;
    }

    struct DataResponse
    {
        public int code;
        public string data;
    }
}