﻿namespace OKAssets
{
    public class OKAssetsConst
    {
        public static OKAssetsConfig okConfig;
        public const string BundleMapFlieName = "BundleMapFile.txt";
        public const string ASSETBUNDLE_FOLDER = "AssetBundles";
        public const string VARIANT = ".ab";
        public const string ASSET_PATH_PREFIX = "Assets/Res/";
    }

    public enum ResLoadMode
    {
        EditorModel,
        OnLineModel,
    }

    public enum GameMode
    {
        DEBUG,
        RELEASE
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