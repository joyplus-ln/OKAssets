﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OKAssets
{
    public class OKResInfoUtil
    {
        public static string CdnUrl = "";

        /// <summary>
        /// 重写本地的bundle信息记录文件
        /// </summary>
        /// <param name="_storageBundlesInfo"></param>
        public static void WriteBundlesInfoToFilesTxt(Dictionary<string, BundleInfo> _BundlesInfo)
        {
            List<string> contentList = new List<string>();
            foreach (BundleInfo info in _BundlesInfo.Values)
            {
                contentList.Add(info.Output());
            }

            string path = Util.GetBundlesInfoConfigPersistentDataPath();
            if (File.Exists(path)) File.Delete(path);
            File.WriteAllLines(path, contentList.ToArray());
        }

        public static void UpdateBundleInfo(Dictionary<string, BundleInfo> _BundlesInfo, BundleInfo newInfo)
        {
            BundleInfo oldBundleInfo = null;
            if (_BundlesInfo.TryGetValue(newInfo.name, out oldBundleInfo))
            {
                oldBundleInfo.Update(newInfo);
            }
            else
            {
                _BundlesInfo.Add(newInfo.name, newInfo);
            }
        }

        public void CacheCDNBundleInfo(string cdnFilesURL, Dictionary<string, BundleInfo> bundleInfos,
            OKAsset.OnCompleteDelegate onCompleteDelegate)
        {
            TextLoader cdnFilesTxtLoader = new TextLoader();
            cdnFilesTxtLoader.Url = cdnFilesURL;
            cdnFilesTxtLoader.OnLoadComplete = delegate(BaseLoader l)
            {
                string cdnFilesStr = cdnFilesTxtLoader.Text;
                string[] cdnFiles = cdnFilesStr.Split('\n');
                for (int i = 0; i < cdnFiles.Length; i++)
                {
                    string cdnFile = cdnFiles[i];
                    if (string.IsNullOrEmpty(cdnFile))
                    {
                        continue;
                    }

                    BundleInfo cdnBundleInfo = new BundleInfo();
                    cdnBundleInfo.Parse(cdnFile);
                    //解析的时候顺便放入本地存一份CDN上files.txt信息
                    UpdateBundleInfo(bundleInfos, cdnBundleInfo);
                }

                if (onCompleteDelegate != null)
                {
                    onCompleteDelegate();
                }
            };
            cdnFilesTxtLoader.Load();
        }


        /// <summary>
        /// 把bundle写到本地，并更新本地的bundleinfo文件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="loader"></param>
        public static void WriteBundleToStorageAndUpdateBundleInfo(string name,
            Dictionary<string, BundleInfo> _cdnBundlesInfo,
            AssetBundleLoader loader)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.OnLineModel)
                return;
            if (loader == null)
                return;
            BundleInfo cdnBundleInfo = null;
            if (_cdnBundlesInfo.TryGetValue(name, out cdnBundleInfo))
            {
                if (cdnBundleInfo.location == BundleStorageLocation.CDN)
                {
                    UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, cdnBundleInfo);
                    BundleInfo storageBundleInfo = GetBundleInfo(name);
                    if (storageBundleInfo != null)
                    {
                        storageBundleInfo.location = BundleStorageLocation.STORAGE;
                        //本地写文件
                        string path = Path.Combine(Util.DataPath, storageBundleInfo.name);
                        if (File.Exists(path)) File.Delete(path);
                        File.WriteAllBytes(path, (byte[])loader.Content);
                        WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
                    }
                }
            }
        }

        public static void WriteBundleToStorageAndUpdateBundleInfo(string name, AssetBundleLoader loader)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.OnLineModel)
                return;
            if (loader == null)
                return;
            BundleInfo cdnBundleInfo = null;
            if (OKAsset.GetInstance().StorageBundlesInfo.TryGetValue(name, out cdnBundleInfo))
            {
                if (cdnBundleInfo.location == BundleStorageLocation.CDN)
                {
                    UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, cdnBundleInfo);
                    BundleInfo storageBundleInfo = GetBundleInfo(name);
                    if (storageBundleInfo != null)
                    {
                        storageBundleInfo.location = BundleStorageLocation.STORAGE;
                        //本地写文件
                        string path = Path.Combine(Util.DataPath, storageBundleInfo.name);
                        if (File.Exists(path)) File.Delete(path);
                        File.WriteAllBytes(path, (byte[])loader.Content);
                        WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
                    }
                }
            }
        }

        public static BundleInfo GetBundleInfo(string name, Dictionary<string, BundleInfo> _BundlesInfo = null)
        {
            BundleInfo bundleInfo = null;
            if (_BundlesInfo == null)
            {
                _BundlesInfo = OKAsset.GetInstance().StorageBundlesInfo;
            }

            if (_BundlesInfo.TryGetValue(name, out bundleInfo))
            {
                return bundleInfo;
            }

            return null;
        }


        public string GetAssetBundlesCdnPath()
        {
            string platform = "Windows";
#if UNITY_IOS
        platform = "iOS";
#elif UNITY_ANDROID
            platform = "Android";
#endif
            if (OKAssetsConst.okConfig.gameMode == GameMode.DEBUG)
            {
                return @$"{CdnUrl}/debug/{Application.version}/{platform}";
            }
            else
            {
                return @$"{CdnUrl}/release/{Application.version}/{platform}";
            }
        }

        public static List<BundleInfo> GetCDNBundlesByTags(string tag)
        {
            List<BundleInfo> bundles = new List<BundleInfo>();
            foreach (string key in OKAsset.GetInstance().StorageBundlesInfo.Keys)
            {
                if (OKAsset.GetInstance().StorageBundlesInfo[key].bundleTag == tag &&
                    OKAsset.GetInstance().StorageBundlesInfo[key].location == BundleStorageLocation.CDN)
                {
                    bundles.Add(OKAsset.GetInstance().StorageBundlesInfo[key]);
                }
            }

            return bundles;
        }


        public static Dictionary<string, string> LoadBundlesTable()
        {
            Dictionary<string, string> bundleTable = new Dictionary<string, string>();
            string bundletablepath = Util.DataPath + OKAssetsConst.BundleMapFlieName;
            //解析bundle和文件的对应表
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
#if UNITY_ANDROID
                bundletablepath = Util.DataPath + $"AssetBundles/Android/{OKAssetsConst.BundleMapFlieName}";
#elif UNITY_IOS
					bundletablepath = Util.DataPath + $"AssetBundles/iOS/{OKAssetsConst.BundleMapFlieName}";
#else
                bundletablepath = Util.DataPath + $"AssetBundles/Windows/{OKAssetsConst.BundleMapFlieName}";
#endif
            }

            string[] files = File.ReadAllLines(bundletablepath);
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string[] fs = file.Split('|');
                if (string.IsNullOrEmpty(fs[0]))
                    continue;
                bundleTable[fs[0]] = fs[1];
            }

            return bundleTable;
        }

        internal static AssetBundleManifest LoadManifest()
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
                return null;
            }

            string path = GetAssetBundlePath(Util.GetPlatformName(), false);
            AssetBundle ab = AssetBundle.LoadFromFile(path);
            AssetBundleManifest assetBundleManifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

            ab.Unload(false);
            ab = null;
            return assetBundleManifest;
        }

        internal static string GetAssetBundlePath(string assetBundleName, bool autoCompletionExts = true)
        {
            string s = "";
            string bundleName = assetBundleName;
            if (autoCompletionExts)
                assetBundleName += OKAssetsConst.VARIANT;

            BundleInfo bundleInfo = OKResInfoUtil.GetBundleInfo(assetBundleName);
            if (bundleInfo != null)
            {
                if (bundleInfo.location == BundleStorageLocation.STREAMINGASSETS)
                {
                    s = Util.GetAssetBundleStreamingAssetsPath();
                }
                else if (bundleInfo.location == BundleStorageLocation.STORAGE)
                {
                    s = GetAssetBundleStoragePath();
                }
                else if (bundleInfo.location == BundleStorageLocation.CDN)
                {
                    s = GetDefaultAssetBundlesCDNPath();
                }

                bundleName = bundleInfo.nameWithHash;
                if (!bundleName.Contains(OKAssetsConst.VARIANT) && autoCompletionExts)
                {
                    bundleName += OKAssetsConst.VARIANT;
                }
            }
            else
            {
                s = Util.GetAssetBundleStreamingAssetsPath();
            }

            s = Path.Combine(s, bundleName);
            return s;
        }



        internal static string GetAssetBundleStoragePath()
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
                return Util.DataPath;
            }

            return Util.DataPath;
        }

        internal static string GetDefaultAssetBundlesCDNPath()
        {
            if (OKAssetsConst.okConfig.gameMode == GameMode.DEBUG)
            {
                return GetFilePath(CdnUrl, OKAssetsConst.okConfig.CDN_DEBUGFOLDER);
            }
            else
            {
                return GetFilePath(CdnUrl, OKAssetsConst.okConfig.CDN_RELEASEFOLDER);
            }
        }

        internal static string GetFilePath(string path, string releaseType)
        {
            string platform = "Windows";
#if UNITY_IOS
        platform = "iOS";
#elif UNITY_ANDROID
            platform = "Android";
#endif
            return path + "/" + releaseType + "/" + Application.version + "/" + platform;
        }
    }
}