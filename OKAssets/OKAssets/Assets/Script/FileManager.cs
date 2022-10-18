using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace OKAssets
{
    public class OKFileManager
    {
        public delegate void OnLoadQueueProgressDelegate(LoaderQueue queue);

        public delegate void OnLoadQueueCompleteDelegate(LoaderQueue queue);
        
        public delegate void OnErrorDelegate();

 

        public delegate void OnCompareCDNBuildVersionResult(bool needDownloadapp);

        //存储在本地硬盘空间的assetbundle文件
        private Dictionary<string, BundleInfo> _storageBundlesInfo = new Dictionary<string, BundleInfo>();

        //CDN上的assetbundle信息
        private Dictionary<string, BundleInfo> _cdnBundlesInfo = new Dictionary<string, BundleInfo>();
        private bool _initalizedCDNBundlesInfo = false;
        private static OKFileManager _instance;
        private bool _writeBundleInfoDirty = false;

        private float _writeBundleInfoTime = 0f;

        public static OKFileManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new OKFileManager();
            }

            return _instance;
        }

        public Dictionary<string, BundleInfo> StorageBundlesInfo
        {
            get { return _storageBundlesInfo; }

            set { _storageBundlesInfo = value; }
        }

        public Dictionary<string, BundleInfo> CDNBundlesInfo
        {
            get { return _cdnBundlesInfo; }

            set { _cdnBundlesInfo = value; }
        }


       

        public void LoadBundlesInfo()
        {
            //解析files.txt获取fileInfo
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.OnLineModel)
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
                    if (!_storageBundlesInfo.ContainsKey(bundleInfo.name))
                    {
                        _storageBundlesInfo.Add(bundleInfo.name, bundleInfo);
                    }
                    else
                    {
                        _storageBundlesInfo[bundleInfo.name] = bundleInfo;
                    }
                }
            }
        }
        
        

        public void CompareCDNBuildVersion(string cdnBuildVersionURL, OnCompareCDNBuildVersionResult onCompareResult,
            OnErrorDelegate onError)
        {
            //检查版本号
            Version cdnVersion = null;
            Version storageVersion = null;
            LoaderQueue loaderQueue = new LoaderQueue();
            TextLoader cdnBuildVersionLoader = new TextLoader();
            cdnBuildVersionLoader.TimeOut = 2;
            cdnBuildVersionLoader.Url = cdnBuildVersionURL;
            cdnBuildVersionLoader.OnLoadComplete = delegate(BaseLoader loader)
            {
                cdnVersion = new Version(cdnBuildVersionLoader.Text);
            };
            loaderQueue.AddLoader(cdnBuildVersionLoader);

            TextLoader storageBuildVersionLoader = new TextLoader();
            storageBuildVersionLoader.Url = Util.GetBuildVersionConfigPersistentDataPath();
            storageBuildVersionLoader.OnLoadComplete = delegate(BaseLoader loader)
            {
                storageVersion = new Version(storageBuildVersionLoader.Text);
            };
            loaderQueue.AddLoader(storageBuildVersionLoader);
            loaderQueue.OnLoadComplete = delegate(LoaderQueue queue)
            {
                bool needDownloadApp = false;
                bool needDownloadBundle = false;
                if (cdnVersion != null && storageVersion != null)
                {
                    if (storageVersion.Build != cdnVersion.Build)
                    {
                        needDownloadApp = true;
                    }
                }

                if (onCompareResult != null)
                {
                    onCompareResult(needDownloadApp);
                }
            };
            loaderQueue.OnLoadError = delegate(LoaderQueue queue)
            {
                if (onError != null)
                {
                    onError();
                }
            };
            loaderQueue.Load();
        }
        





        public BundleInfo GetBundleInfo(string name)
        {
            BundleInfo bundleInfo = null;
            if (_storageBundlesInfo.TryGetValue(name, out bundleInfo))
            {
                return bundleInfo;
            }

            return null;
        }







        public bool HasBundleInfoOnStorage(string name)
        {
            BundleInfo bundleInfo = GetBundleInfo(name);
            if (bundleInfo != null && (bundleInfo.location == BundleStorageLocation.STORAGE ||
                                       bundleInfo.location == BundleStorageLocation.STREAMINGASSETS))
            {
                return true;
            }

            return false;
        }
        


        

        

        public BundleInfo[] GetStorageBundleInfoListBySubpackage(string tag)
        {
            List<BundleInfo> result = new List<BundleInfo>();
            foreach (BundleInfo info in _storageBundlesInfo.Values)
            {
                if (info.bundleTag == tag)
                {
                    result.Add(info);
                }
            }

            return result.ToArray();
        }

        public BundleInfo[] GetBundleInfoListDifferFromCDNBundleInfo(BundleInfo[] bundlesInfo)
        {
            List<BundleInfo> result = new List<BundleInfo>();
            foreach (BundleInfo item in bundlesInfo)
            {
                BundleInfo cdnBundleInfo = null;
                if (_cdnBundlesInfo.ContainsKey(item.name))
                    cdnBundleInfo = _cdnBundlesInfo[item.name];

                if (cdnBundleInfo == null)
                    continue;

                if (item.location == BundleStorageLocation.CDN || !item.crcOrMD5Hash.Equals(cdnBundleInfo.crcOrMD5Hash))
                {
                    result.Add(item);
                }
            }

            return result.ToArray();
        }

        public bool HasCacheFile(string fileName)
        {
            string path = Path.Combine(Util.DataPath, fileName);
            return File.Exists(path);
        }

        public void DownLoadFile(string filePath, string fileName, Action<byte[]> complete)
        {
            BinaryLoader fileLoader = new BinaryLoader();
            fileLoader.Url = filePath + "/" + fileName;
            fileLoader.OnLoadComplete = delegate(BaseLoader loader)
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

        public byte[] ReadCacheFile(string fileName)
        {
            string path = Path.Combine(Util.DataPath, fileName);
            return File.ReadAllBytes(path);
        }
    }
}