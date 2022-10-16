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

        public delegate void OnItemCompleteDelegate();

        public delegate void OnCompleteDelegate();

        public delegate void OnErrorDelegate();

        public delegate void OnCompareCDNResult(BundleInfo[] diffFilesInfo, float totalByteSize);

        public delegate void OnCompareCDNBuildVersionResult(bool needDownloadapp);

        //存储在本地硬盘空间的assetbundle文件
        private Dictionary<string, BundleInfo> _storageBundlesInfo = new Dictionary<string, BundleInfo>();

        //CDN上的assetbundle信息
        private Dictionary<string, BundleInfo> _cdnBundlesInfo = new Dictionary<string, BundleInfo>();
        private bool _initalizedCDNBundlesInfo = false;
        private static OKFileManager _instance;
        private bool _writeBundleInfoDirty = false;

        private float _writeBundleInfoTime = 0f;

        //线上
        public string cdnBuildVersion = "";

        //包体里的version
        public string storageBuildVersion = "";

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



        public string GetBundlesInfoConfigStreamingAssetsPath()
        {
            return Path.Combine(OKResManager.GetInstance().GetAssetBundleStreamingAssetsPath(), OKAssetsConst.FILENAME_FILES_TXT);
        }

        public string GetBundlesInfoConfigPersistentDataPath()
        {
            return Path.Combine(Util.DataPath, OKAssetsConst.FILENAME_FILES_TXT);
        }

        public string GetBuildVersionConfigStreamingAssetsPath()
        {
            return Path.Combine(OKResManager.GetInstance().GetAssetBundleStreamingAssetsPath(),
                OKAssetsConst.FILENAME_BUILDVERSION_TXT);
        }

        public string GetBuildVersionConfigPersistentDataPath()
        {
            return Path.Combine(Util.DataPath, OKAssetsConst.FILENAME_BUILDVERSION_TXT);
        }

        public string GetBundlesTableConfigStreamingAssetsPath()
        {
            return Path.Combine(OKResManager.GetInstance().GetAssetBundleStreamingAssetsPath(),
                OKAssetsConst.FILENAME_BUNDLESTABLE_TXT);
        }

        public string GetBundlesTableConfigPersistentDataPath()
        {
            return Path.Combine(Util.DataPath, OKAssetsConst.FILENAME_BUNDLESTABLE_TXT);
        }

        public string GetAppConfigJSONPath()
        {
            return Path.Combine(Application.streamingAssetsPath, OKAssetsConst.FILENAME_APPCONFIG_JSON);
        }

        public void LoadBundlesInfo()
        {
            //解析files.txt获取fileInfo
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.OnLineModel)
            {
                string[] files = File.ReadAllLines(GetBundlesInfoConfigPersistentDataPath());
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

        public void UpdateStorageBundleInfo(BundleInfo newInfo)
        {
            BundleInfo oldBundleInfo = null;
            if (_storageBundlesInfo.TryGetValue(newInfo.name, out oldBundleInfo))
            {
                oldBundleInfo.Update(newInfo);
            }
            else
            {
                _storageBundlesInfo.Add(newInfo.name, newInfo);
            }
        }

        public void UpdateCDNBundleInfo(BundleInfo newInfo)
        {
            BundleInfo oldBundleInfo = null;
            if (_cdnBundlesInfo.TryGetValue(newInfo.name, out oldBundleInfo))
            {
                oldBundleInfo.Update(newInfo);
            }
            else
            {
                _cdnBundlesInfo.Add(newInfo.name, newInfo);
            }
        }

        public void CacheCDNBundleInfo(string cdnFilesURL, OnCompleteDelegate onCompleteDelegate)
        {
            if (_initalizedCDNBundlesInfo)
            {
                if (onCompleteDelegate != null)
                {
                    onCompleteDelegate();
                }

                return;
            }

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
                    UpdateCDNBundleInfo(cdnBundleInfo);
                }

                _initalizedCDNBundlesInfo = true;
                if (onCompleteDelegate != null)
                {
                    onCompleteDelegate();
                }
            };
            cdnFilesTxtLoader.Load();
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
                cdnBuildVersion = cdnVersion.ToString();
            };
            loaderQueue.AddLoader(cdnBuildVersionLoader);

            TextLoader storageBuildVersionLoader = new TextLoader();
            storageBuildVersionLoader.Url = OKFileManager.GetInstance().GetBuildVersionConfigPersistentDataPath();
            storageBuildVersionLoader.OnLoadComplete = delegate(BaseLoader loader)
            {
                storageVersion = new Version(storageBuildVersionLoader.Text);
                storageBuildVersion = storageVersion.ToString();
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

        public void CompareFiles(string cdnFilesURL, OnCompareCDNResult onCompareResult)
        {
            List<BundleInfo> diffFilesInfoList = new List<BundleInfo>();
            long totalByteSize = 0;
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
                    UpdateCDNBundleInfo(cdnBundleInfo);
                    bool hasDiff = false;
                    //获取目前本地的
                    BundleInfo oldBundleInfo = GetBundleInfo(cdnBundleInfo.name);


                    //如果本地已经有这个记录了
                    if (oldBundleInfo != null)
                    {
                        //本地的全部需要下载
                        if (cdnBundleInfo.loactionType == BundleLocation.Local)
                        {
                            //最后检查crc是否需要更新
                            if (oldBundleInfo.crcOrMD5Hash.Equals(cdnBundleInfo.crcOrMD5Hash) == false)
                            {
                                hasDiff = true;
                            }
                        }

                        //先比对一下tag，如果tag不一样就先修改一下tag
                        if (oldBundleInfo.bundleTag.Equals(cdnBundleInfo.bundleTag) == false)
                        {
                            if (hasDiff)
                            {
                                UpdateStorageBundleInfo(cdnBundleInfo);
                            }
                            else
                            {
                                oldBundleInfo.bundleTag = cdnBundleInfo.bundleTag;
                                UpdateStorageBundleInfo(oldBundleInfo);
                            }

                            SetWriteBundleInfoDirty();
                        }
                    }
                    else
                    {
                        //先检查存储位置，必须是在streamingPath或者dataPath存储
                        if (cdnBundleInfo.loactionType == BundleLocation.Local)
                        {
                            hasDiff = true;
                        }

                        //如果本地没有，那么查看这个是不是CDN位置的 如果是把记录更新
                        if (cdnBundleInfo.location == BundleStorageLocation.CDN)
                        {
                            UpdateStorageBundleInfo(cdnBundleInfo);
                            SetWriteBundleInfoDirty();
                        }
                    }

                    if (hasDiff)
                    {
                        BundleInfo newBundleInfo = new BundleInfo();
                        newBundleInfo.Update(cdnBundleInfo);
                        newBundleInfo.location = BundleStorageLocation.CDN;
                        diffFilesInfoList.Add(newBundleInfo);
                    }
                }

                _initalizedCDNBundlesInfo = true;
                for (int i = 0; i < diffFilesInfoList.Count; i++)
                {
                    totalByteSize += diffFilesInfoList[i].byteSize;
                }

                if (onCompareResult != null)
                {
                    onCompareResult(diffFilesInfoList.ToArray(), (float)totalByteSize);
                }
            };
            cdnFilesTxtLoader.Load();
        }

        public void DownloadFilesByBundleInfo(string[] prefixURL, BundleInfo[] downloadInfoArray,
            OnLoadQueueProgressDelegate onProgress, OnLoadQueueCompleteDelegate onComplete)
        {
            LoaderQueue queue = new LoaderQueue();
            for (int i = 0; i < downloadInfoArray.Length; i++)
            {
                BundleInfo info = downloadInfoArray[i];
                string fileURL = Path.Combine(prefixURL[0], info.nameWithHash);
                string[] fileURLs = new string[prefixURL.Length];
                for (int j = 0; j < fileURLs.Length; j++)
                {
                    fileURLs[j] = Path.Combine(prefixURL[j], info.nameWithHash);
                }

                BinaryLoader loader = new BinaryLoader();
                loader.Name = info.name;
                loader.Url = fileURL;
                loader.OnLoadComplete = delegate(BaseLoader l)
                {
                    BundleInfo finishInfo = GetBundleInfoFormArray(downloadInfoArray, l.Name);
                    finishInfo.location = BundleStorageLocation.STORAGE;
                    UpdateStorageBundleInfo(finishInfo);
                    string path = Path.Combine(Util.DataPath, finishInfo.nameWithHash);
                    if (File.Exists(path)) File.Delete(path);
                    File.WriteAllBytes(path, (byte[])l.Content);
                    SetWriteBundleInfoDirty();
                };
                queue.AddLoader(loader);
            }

            queue.OnLoadProgress += delegate(LoaderQueue q)
            {
                if (onProgress != null)
                {
                    onProgress(q);
                }
            };
            queue.OnLoadComplete = delegate(LoaderQueue q)
            {
                WriteStorageBundlesInfoToFilesTxt();
                if (onComplete != null)
                {
                    onComplete(q);
                }
            };
            queue.Load();
        }

        /// <summary>
        /// 根据tag下载
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="onProgress"></param>
        /// <param name="onComplete"></param>
        public void DownloadBundleByTag(string tag, Action<float, float, int, int> progress, Action<bool> complete)
        {
            var prefixURL = OKResManager.GetInstance().GetAssetBundlesCDNPath();
            var downloadInfoArray = GetCDNBundlesByTags(tag).ToArray();
            float totalSize = GetSizeByTags(tag);
            LoaderQueue queue = new LoaderQueue();
            for (int i = 0; i < downloadInfoArray.Length; i++)
            {
                BundleInfo info = downloadInfoArray[i];
                string fileURL = Path.Combine(prefixURL[0], info.name);
                string[] fileURLs = new string[prefixURL.Length];
                for (int j = 0; j < fileURLs.Length; j++)
                {
                    fileURLs[j] = Path.Combine(prefixURL[j], info.name);
                }

                BinaryLoader loader = new BinaryLoader();
                loader.Name = info.name;
                loader.Url = fileURL;
                loader.OnLoadComplete = delegate(BaseLoader l)
                {
                    BundleInfo finishInfo = GetBundleInfoFormArray(downloadInfoArray, l.Name);
                    finishInfo.location = BundleStorageLocation.STORAGE;
                    UpdateStorageBundleInfo(finishInfo);
                    string path = Path.Combine(Util.DataPath, finishInfo.name);
                    if (File.Exists(path)) File.Delete(path);
                    File.WriteAllBytes(path, (byte[])l.Content);
                    SetWriteBundleInfoDirty();
                };
                queue.AddLoader(loader);
            }

            queue.OnLoadProgress += delegate(LoaderQueue q)
            {
                if (progress != null)
                {
                    progress.Invoke(totalSize, q.ProgressByteSize, q.TotalLoadCount, q.CurrentLoadedCount);
                }
            };
            queue.OnLoadComplete = delegate(LoaderQueue q)
            {
                WriteStorageBundlesInfoToFilesTxt();
                if (complete != null)
                {
                    complete.Invoke(true);
                }
            };
            queue.Load();
        }


        private BundleInfo GetBundleInfoFormArray(BundleInfo[] infoArray, string name)
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

        public BundleInfo GetBundleInfo(string name)
        {
            BundleInfo bundleInfo = null;
            if (_storageBundlesInfo.TryGetValue(name, out bundleInfo))
            {
                return bundleInfo;
            }

            return null;
        }

        /// <summary>
        /// 根据tag获取所有bundle
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<BundleInfo> GetBundlesByTags(string tag)
        {
            List<BundleInfo> bundles = new List<BundleInfo>();
            foreach (string key in _storageBundlesInfo.Keys)
            {
                if (_storageBundlesInfo[key].bundleTag == tag)
                {
                    bundles.Add(_storageBundlesInfo[key]);
                }
            }

            return bundles;
        }

        public List<BundleInfo> GetCDNBundlesByTags(string tag)
        {
            List<BundleInfo> bundles = new List<BundleInfo>();
            foreach (string key in _storageBundlesInfo.Keys)
            {
                if (_storageBundlesInfo[key].bundleTag == tag &&
                    _storageBundlesInfo[key].location == BundleStorageLocation.CDN)
                {
                    bundles.Add(_storageBundlesInfo[key]);
                }
            }

            return bundles;
        }

        /// <summary>
        /// 获取tag的下载量
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public float GetSizeByTags(string tag)
        {
            List<BundleInfo> bundles = GetBundlesByTags(tag);
            float size = 0;
            for (int i = 0; i < bundles.Count; i++)
            {
                if (bundles[i].location == BundleStorageLocation.CDN)
                {
                    size += bundles[i].byteSize;
                }
            }

            return size;
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

        public void WriteStorageBundlesInfoToFilesTxt()
        {
            List<string> contentList = new List<string>();
            foreach (BundleInfo info in _storageBundlesInfo.Values)
            {
                contentList.Add(info.Output());
            }

            string path = GetBundlesInfoConfigPersistentDataPath();
            if (File.Exists(path)) File.Delete(path);
            File.WriteAllLines(path, contentList.ToArray());
        }

        //每个30秒检查一次是否需要重写写入files.txt
        public void OnTick()
        {
            if (Time.time - _writeBundleInfoTime <= 15)
            {
                return;
            }

            _writeBundleInfoTime = Time.time;
            if (_writeBundleInfoDirty)
            {
                _writeBundleInfoDirty = false;
                WriteStorageBundlesInfoToFilesTxt();
            }
        }

        public void OnLateTick()
        {
        }

        public void SetWriteBundleInfoDirty()
        {
            _writeBundleInfoDirty = true;
        }

        public void WriteBundleToStorageAndUpdateBundleInfo(string name, AssetBundleLoader loader)
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
                    UpdateStorageBundleInfo(cdnBundleInfo);
                    BundleInfo storageBundleInfo = GetBundleInfo(name);
                    if (storageBundleInfo != null)
                    {
                        storageBundleInfo.location = BundleStorageLocation.STORAGE;
                        SetWriteBundleInfoDirty();
                        //本地写文件
                        string path = Path.Combine(Util.DataPath, storageBundleInfo.name);
                        if (File.Exists(path)) File.Delete(path);
                        File.WriteAllBytes(path, (byte[])loader.Content);
                    }
                }
            }
        }

        public void CopyFile(string sourceFile, string destFolderPath, OnCompleteDelegate onComplete = null)
        {
            bool isFolder = string.IsNullOrEmpty(Path.GetFileName(destFolderPath)) ? true : false;
            string sourceFilePath = sourceFile;
            string sourceFileName = Path.GetFileName(sourceFilePath);
            string destFile = "";
            if (isFolder)
            {
                destFile = Path.Combine(destFolderPath, sourceFileName);
            }
            else
            {
                destFile = destFolderPath;
            }

            if (File.Exists(destFile)) File.Delete(destFile);
            if (Util.IsAndroid())
            {
                BinaryLoader fileLoader = new BinaryLoader();
                fileLoader.Url = sourceFilePath;
                fileLoader.OnLoadComplete = delegate(BaseLoader loader)
                {
                    File.WriteAllBytes(destFile, (byte[])loader.Content);
                    if (onComplete != null)
                    {
                        onComplete();
                    }
                };
                fileLoader.Load();
            }
            else
            {
                File.Copy(sourceFilePath, destFile, true);
                if (onComplete != null)
                {
                    onComplete();
                }
            }
        }

        public void CopyFiles(string[] sourceFilePathArray, string destFolderPath, OnCompleteDelegate onComplete = null,
            OnItemCompleteDelegate onItemComplete = null)
        {
            bool isFolder = string.IsNullOrEmpty(Path.GetFileName(destFolderPath)) ? true : false;

            LoaderQueue queue = new LoaderQueue();
            for (int i = 0; i < sourceFilePathArray.Length; i++)
            {
                string sourceFilePath = sourceFilePathArray[i];
                string sourceFileName = Path.GetFileName(sourceFilePath);
                string destFile = "";
                if (isFolder)
                {
                    destFile = Path.Combine(destFolderPath, sourceFileName);
                }
                else
                {
                    destFile = destFolderPath;
                }

                if (File.Exists(destFile)) File.Delete(destFile);
                if (Util.IsAndroid())
                {
                    BinaryLoader fileLoader = new BinaryLoader();
                    fileLoader.Url = sourceFilePath;
                    fileLoader.OnLoadComplete = delegate(BaseLoader loader)
                    {
                        File.WriteAllBytes(destFile, (byte[])loader.Content);
                        if (onItemComplete != null)
                        {
                            onItemComplete();
                        }
                    };
                    queue.AddLoader(fileLoader);
                }
                else
                {
                    File.Copy(sourceFilePath, destFile, true);
                }
            }

            if (Util.IsAndroid())
            {
                queue.OnLoadComplete = delegate(LoaderQueue q)
                {
                    if (onComplete != null)
                    {
                        onComplete();
                    }
                };
                queue.Load();
            }
            else
            {
                if (onComplete != null)
                {
                    onComplete();
                }
            }
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