using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OKAssets
{
    public class OKResUtil
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
        
        internal static void LoadBundlesInfo()
        {
            //解析files.txt获取fileInfo
            if (Util.IsOnLineModel())
            {
                string[] files = File.ReadAllLines(Util.GetBundlesInfoConfigPersistentDataPath());
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    if (string.IsNullOrEmpty(file))
                    {
                        continue;
                    }

                    BundleInfo bundleInfo = new BundleInfo();
                    bundleInfo.Parse(file);
                    if (!OKAsset.GetInstance().StorageBundlesInfo.ContainsKey(bundleInfo.name))
                    {
                        OKAsset.GetInstance().StorageBundlesInfo.Add(bundleInfo.name, bundleInfo);
                    }
                    else
                    {
                        OKAsset.GetInstance().StorageBundlesInfo[bundleInfo.name] = bundleInfo;
                    }
                }
            }
        }

        public void CacheCDNBundleInfo(string cdnFilesURL, Dictionary<string, BundleInfo> bundleInfos,
            OKAsset.OnCompleteDelegate onCompleteDelegate)
        {
            TextLoader cdnFilesTxtLoader = new TextLoader();
            cdnFilesTxtLoader.Url = cdnFilesURL;
            cdnFilesTxtLoader.OnLoadComplete = delegate(OKBaseLoader l)
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
            OKAssetBundleLoader loader)
        {
            if (!Util.IsOnLineModel())
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

        public static void WriteBundleToStorageAndUpdateBundleInfo(string name, OKAssetBundleLoader loader)
        {
            if (!Util.IsOnLineModel())
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
        

        internal static AssetBundleManifest LoadManifest()
        {
            if (!Util.IsOnLineModel())
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
            if (autoCompletionExts && !bundleName.Contains(OKAssetsConst.VARIANT))
                assetBundleName += OKAssetsConst.VARIANT;

            BundleInfo bundleInfo = OKResUtil.GetBundleInfo(assetBundleName);
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
                    s = CdnUrl;
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
            if (!Util.IsOnLineModel())
            {
                return Util.DataPath;
            }

            return Util.DataPath;
        }
        

        internal static BundleInfo GetBundleInfoFormArray(BundleInfo[] infoArray, string name)
        {
            for (int i = 0; i < infoArray.Length; i++)
            {
                if (infoArray[i].name.Equals(name))
                {
                    return infoArray[i];
                }
            }

            return null;
        }


        /// <summary>
        /// 加载text格式的文件
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="successCallBack"></param>
        /// <param name="errorCallBack"></param>
        public static void LoadTextAsset(string URL, Action<string> successCallBack, Action errorCallBack)
        {
            TextLoader filesTxtLoader = new TextLoader();
            filesTxtLoader.Url = URL;
            filesTxtLoader.OnLoadComplete = delegate(OKBaseLoader loader)
            {
                successCallBack?.Invoke(filesTxtLoader.Text);
            };
            filesTxtLoader.OnLoadError = delegate(OKBaseLoader loader) { errorCallBack?.Invoke(); };
            filesTxtLoader.Load();
        }

        public static void LoadBinaryAsset(string URL, Action<byte[]> successCallBack, Action errorCallBack)
        {
            BinaryLoader filesTxtLoader = new BinaryLoader();
            filesTxtLoader.Url = URL;
            filesTxtLoader.OnLoadComplete = delegate(OKBaseLoader loader)
            {
                successCallBack?.Invoke((byte[])filesTxtLoader.Content);
            };
            filesTxtLoader.OnLoadError = delegate(OKBaseLoader loader) { errorCallBack?.Invoke(); };
            filesTxtLoader.Load();
        }


        public static void LoadBinaryAssets(string[] URLs, Action<string, byte[]> successCallBack, Action errorCallBack)
        {
            OKLoaderQueue queue = new OKLoaderQueue();
            for (int i = 0; i < URLs.Length; i++)
            {
                BinaryLoader filesTxtLoader = new BinaryLoader();
                filesTxtLoader.Url = URLs[i];
                filesTxtLoader.OnLoadComplete = delegate(OKBaseLoader loader)
                {
                    successCallBack?.Invoke(URLs[i], (byte[])filesTxtLoader.Content);
                };
                filesTxtLoader.OnLoadError = delegate(OKBaseLoader loader) { errorCallBack?.Invoke(); };
                queue.AddLoader(filesTxtLoader);
            }

            queue.Load();
        }

        public static void DownloadFilesByBundleInfo(BundleInfo[] downloadInfoArray,
            OKFileManager.OnLoadQueueProgressDelegate onProgress, Action<List<BundleInfo>> downLoadFailCallBack)
        {
            List<BundleInfo> loadFailedList = new List<BundleInfo>();
            OKLoaderQueue queue = new OKLoaderQueue();
            for (int i = 0; i < downloadInfoArray.Length; i++)
            {
                BundleInfo info = downloadInfoArray[i];
                string fileURL = Path.Combine(CdnUrl, info.nameWithHash);
                BinaryLoader loader = new BinaryLoader();
                loader.Name = info.name;
                loader.Url = fileURL;
                loader.OnLoadComplete = delegate(OKBaseLoader l)
                {
                    BundleInfo finishInfo = GetBundleInfoFormArray(downloadInfoArray, l.Name);
                    finishInfo.location = BundleStorageLocation.STORAGE;
                    UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, finishInfo);
                    string path = Path.Combine(Util.DataPath, finishInfo.nameWithHash);
                    if (File.Exists(path)) File.Delete(path);
                    File.WriteAllBytes(path, (byte[])l.Content);
                    WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
                };
                loader.OnLoadError = delegate(OKBaseLoader baseLoader) { loadFailedList.Add(info); };
                queue.AddLoader(loader);
            }

            queue.OnLoadProgress += delegate(OKLoaderQueue q)
            {
                if (onProgress != null)
                {
                    onProgress(q);
                }
            };
            queue.OnLoadComplete = delegate(OKLoaderQueue q) { downLoadFailCallBack?.Invoke(loadFailedList); };
            queue.Load();
        }
        
        internal static void DownLoadFile(string filePath, string fileName, Action<byte[]> complete)
        {
            BinaryLoader fileLoader = new BinaryLoader();
            fileLoader.Url = filePath + "/" + fileName;
            fileLoader.OnLoadComplete = delegate(OKBaseLoader loader)
            {
                string path = Path.Combine(Util.DataPath, fileName);
                File.WriteAllBytes(path, (byte[])loader.Content);
                if (complete != null)
                {
                    complete((byte[])loader.Content);
                }
            };
            fileLoader.Load();
        }
    }
}