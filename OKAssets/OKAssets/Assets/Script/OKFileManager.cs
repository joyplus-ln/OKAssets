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
        
        internal static void CompareCDNBuildVersion(string cdnBuildVersionURL, OnCompareCDNBuildVersionResult onCompareResult,
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
        
        internal static void CompareFiles(string cdnFilesURL, FileCompare.OnCompareCDNResult onCompareResult)
        {
            List<BundleInfo> diffFilesInfoList = new List<BundleInfo>();
            long totalByteSize = 0;

            //加载成功
            void LoadSuccessful(string s)
            {
                string cdnFilesStr = s;
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
                    OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, cdnBundleInfo);
                    bool hasDiff = false;
                    //获取目前本地的
                    BundleInfo oldBundleInfo =OKResUtil.GetBundleInfo(cdnBundleInfo.name);


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
                                OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, cdnBundleInfo);
                            }
                            else
                            {
                                oldBundleInfo.bundleTag = cdnBundleInfo.bundleTag;
                                OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, oldBundleInfo);
                            }

                            OKResUtil.WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
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
                            OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, cdnBundleInfo);
                            OKResUtil.WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
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
                for (int i = 0; i < diffFilesInfoList.Count; i++)
                {
                    totalByteSize += diffFilesInfoList[i].byteSize;
                }

                if (onCompareResult != null)
                {
                    onCompareResult(diffFilesInfoList.ToArray(), (float)totalByteSize);
                } 
            }

            //加载失败
            void LoadError()
            {
                
            }
            OKResUtil.LoadTextAsset(cdnFilesURL,LoadSuccessful,LoadError);
        }

        public static BundleInfo[] GetStorageBundleInfoListBySubpackage(string tag)
        {
            List<BundleInfo> result = new List<BundleInfo>();
            foreach (BundleInfo info in OKAsset.GetInstance().StorageBundlesInfo.Values)
            {
                if (info.bundleTag == tag)
                {
                    result.Add(info);
                }
            }

            return result.ToArray();
        }

 
        /// <summary>
        /// 有没有这个文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool HasCacheFile(string fileName)
        {
            string path = Path.Combine(Util.DataPath, fileName);
            return File.Exists(path);
        }



        public byte[] ReadCacheFile(string fileName)
        {
            string path = Path.Combine(Util.DataPath, fileName);
            return File.ReadAllBytes(path);
        }
    }
}