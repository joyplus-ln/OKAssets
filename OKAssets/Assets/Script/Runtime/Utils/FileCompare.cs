using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OKAssets
{
    public class FileCompare
    {
        public delegate void OnCompleteDelegate();

        public delegate void OnItemCompleteDelegate();

        public delegate void OnCompareCDNResult(BundleInfo[] diffFilesInfo, float totalByteSize);
        
        private OnCompleteDelegate complete;
        
        //跟线上的比对
        public void UpdateOnLine(OnCompleteDelegate complete)
        {
            this.complete = complete;
            CheckExtractResource();
        }
        
        private void CopyFilesOut()
        {
            string streamingPath = Util.GetAssetBundleStreamingAssetsPath();
            string dataPath = Util.DataPath;

            List<string> copyFiles = new List<string>();
            List<string> deleteFiles = new List<string>();
            TextLoader loader = new TextLoader();
            loader.Url = Util.GetBundlesInfoConfigStreamingAssetsPath();
            loader.OnLoadComplete = delegate(OKBaseLoader l)
            {
                string streamingAssetsContent = loader.Text;
                if (string.IsNullOrEmpty(streamingAssetsContent))
                {
                    return;
                }

                TextLoader _loader = new TextLoader();
                _loader.Url = Util.GetBundlesInfoConfigPersistentDataPath();
                _loader.OnLoadComplete = delegate(OKBaseLoader l)
                {
                    string storageContent = _loader.Text;
                    if (string.IsNullOrEmpty(storageContent))
                    {
                        return;
                    }

                    string[] streamingAssetsfiles = streamingAssetsContent.Split('\n');
                    string[] storagefiles = storageContent.Split('\n');
                    Dictionary<string, BundleInfo> streamFilesDict = new Dictionary<string, BundleInfo>();
                    Dictionary<string, BundleInfo> storageFilesDict = new Dictionary<string, BundleInfo>();
                    for (int i = 0; i < streamingAssetsfiles.Length; i++)
                    {
                        string file = streamingAssetsfiles[i];
                        if (string.IsNullOrEmpty(file))
                            continue;
                        BundleInfo streamingFBI = new BundleInfo();
                        streamingFBI.Parse(file);
                        string fileName = streamingFBI.name;
                        streamFilesDict[fileName] = streamingFBI;
                    }

                    for (int i = 0; i < storagefiles.Length; i++)
                    {
                        string file = storagefiles[i];
                        if (string.IsNullOrEmpty(file))
                            continue;
                        BundleInfo streamingFBI = new BundleInfo();
                        streamingFBI.Parse(file);
                        string fileName = streamingFBI.name;
                        storageFilesDict[fileName] = streamingFBI;
                    }

                    foreach (string key in streamFilesDict.Keys)
                    {
                        BundleInfo streamingFBI = streamFilesDict[key];
                        if (key.Equals(OKAssetsConst.FILENAME_BUNDLESTABLE_TXT) ||
                            key.Equals(OKAssetsConst.FILENAME_BUILDVERSION_TXT))
                        {
                            copyFiles.Add(Path.Combine(streamingPath, key));
                            //一边更新一遍将安装包的files合并到dataPath的files
                            streamingFBI.location = BundleStorageLocation.STORAGE;
                            OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, streamingFBI);
                        }
                        else
                        {
                            if (streamingFBI.loactionType == BundleLocation.Local)
                            {
                                //新装的包里不是存在CDN上的内容 都用streaming里的 也就是包体里的，并且更新本地DataPath的files.txt与streaming中files.txt内容一致。并且删除DataPath下的旧资源
                                OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, streamingFBI);
                                string path = Path.Combine(Util.DataPath, streamingFBI.name);
                                deleteFiles.Add(path);
                            }
                            else
                            {
                                BundleInfo storageFBI = null;
                                if (storageFilesDict.TryGetValue(key, out storageFBI))
                                {
                                    //老包中也有这个同名bundle,并且md5不一样
                                    if (storageFBI.crcOrMD5Hash != streamingFBI.crcOrMD5Hash)
                                    {
                                        streamingFBI.location = BundleStorageLocation.CDN;
                                        OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, streamingFBI);
                                        string path = Path.Combine(Util.DataPath, streamingFBI.name);
                                        deleteFiles.Add(path);
                                    }
                                    else
                                    {
                                        OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo, storageFBI);
                                    }
                                }
                            }
                        }
                    }

                    //复制刚才那些要更新的文件到dataPath
                    CopyFiles(copyFiles.ToArray(), dataPath, delegate()
                    {
                        //重新写入file.txt
                        OKResUtil.WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
                        for (int i = 0; i < deleteFiles.Count; i++)
                        {
                            if (File.Exists(deleteFiles[i]))
                                File.Delete(deleteFiles[i]);
                        }

                        //释放完成，开始启动ts层面代码
                        this.complete?.Invoke();
                    });
                };
                _loader.Load();
            };
            loader.Load();
        }

        void OnExtractResource()
        {
            //进入到这个方法之后表示，是因为装了新包了，并且新包里的buildversion大于本地的,或者本地根本就没有（第一次安装进入游戏）

            string dataPath = Util.DataPath; //数据目录

            //没有DATAPATH目录就创建一个
            if (Directory.Exists(dataPath) == false)
                Directory.CreateDirectory(dataPath);

            string streamingPath_files_txt = Util.GetBundlesInfoConfigStreamingAssetsPath();
            string dataPath_files_txt = Util.GetBundlesInfoConfigPersistentDataPath();

            if (File.Exists(dataPath_files_txt))
            {
                //已经有files.txt在DataPath目录了
                CopyFilesOut();
            }
            else
            {
                CopyFile(streamingPath_files_txt, dataPath, delegate() { CopyFilesOut(); });
            }
        }


        /// <summary>
        /// /释放资源
        /// </summary>
        private void CheckExtractResource()
        {
            //编辑器模式下，直接启动游戏
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
                return;
            }


            string inBuildVersionFilePath = Util.GetBuildVersionConfigStreamingAssetsPath();
            string outBuildVersionFilePath = Util.GetBuildVersionConfigPersistentDataPath();
            Version inVersion = null;
            TextLoader inBuildVersionLoader = new TextLoader();
            inBuildVersionLoader.Url = inBuildVersionFilePath;
            inBuildVersionLoader.OnLoadComplete = delegate(OKBaseLoader loader)
            {
                Debug.Log(inBuildVersionLoader.Text);
                inVersion = new Version(inBuildVersionLoader.Text);
                if (Directory.Exists(Util.DataPath) == false)
                {
                    OnExtractResource();
                    return;
                }

                bool isExistsBuildVersionFile = File.Exists(outBuildVersionFilePath);
                if (isExistsBuildVersionFile == false)
                {
                    Debug.Log("Can not find old buildversion.txt");
                    OnExtractResource();
                    return;
                }

                //检查版本号
                Version outVersion = null;
                TextLoader outBuildVersionLoader = new TextLoader();
                outBuildVersionLoader.Url = outBuildVersionFilePath;
                outBuildVersionLoader.OnLoadComplete = delegate(OKBaseLoader l)
                {
                    outVersion = new Version(outBuildVersionLoader.Text);
                    if (inVersion != null && outVersion != null)
                    {
                        if ((outVersion.Major != inVersion.Major || outVersion.Minor != inVersion.Minor ||
                             outVersion.Build != inVersion.Build) || (outVersion.Revision < inVersion.Revision &&
                                                                      outVersion.Major == inVersion.Major &&
                                                                      outVersion.Minor == inVersion.Minor &&
                                                                      outVersion.Build == inVersion.Build))
                        {
                            OnExtractResource();
                        }
                        else
                        {
                            this.complete?.Invoke();
                        }
                    }
                };
                outBuildVersionLoader.Load();
            };
            inBuildVersionLoader.Load();
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
                fileLoader.OnLoadComplete = delegate(OKBaseLoader loader)
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

            OKLoaderQueue queue = new OKLoaderQueue();
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
                    fileLoader.OnLoadComplete = delegate(OKBaseLoader loader)
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
                queue.OnLoadComplete = delegate(OKLoaderQueue q)
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


        

        
        
        /// <summary>
        /// 根据tag下载
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="onProgress"></param>
        /// <param name="onComplete"></param>
        public void DownloadBundleByTag(string tag, Action<float, float, int, int> progress, Action<bool> complete)
        {
            var downloadInfoArray = OKResUtil.GetCDNBundlesByTags(tag).ToArray();
            float totalSize = GetSizeByTags(tag);
            OKLoaderQueue queue = new OKLoaderQueue();
            for (int i = 0; i < downloadInfoArray.Length; i++)
            {
                BundleInfo info = downloadInfoArray[i];
                string fileURL = Path.Combine(OKResUtil.CdnUrl, info.name);

                BinaryLoader loader = new BinaryLoader();
                loader.Name = info.name;
                loader.Url = fileURL;
                loader.OnLoadComplete = delegate(OKBaseLoader l)
                {
                    BundleInfo finishInfo = OKResUtil.GetBundleInfoFormArray(downloadInfoArray, l.Name);
                    finishInfo.location = BundleStorageLocation.STORAGE;
                   OKResUtil.UpdateBundleInfo(OKAsset.GetInstance().StorageBundlesInfo,finishInfo);
                    string path = Path.Combine(Util.DataPath, finishInfo.name);
                    if (File.Exists(path)) File.Delete(path);
                    File.WriteAllBytes(path, (byte[])l.Content);
                    OKResUtil.WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
                };
                queue.AddLoader(loader);
            }

            queue.OnLoadProgress += delegate(OKLoaderQueue q)
            {
                if (progress != null)
                {
                    progress.Invoke(totalSize, q.ProgressByteSize, q.TotalLoadCount, q.CurrentLoadedCount);
                }
            };
            queue.OnLoadComplete = delegate(OKLoaderQueue q)
            {
                OKResUtil.WriteBundlesInfoToFilesTxt(OKAsset.GetInstance().StorageBundlesInfo);
                if (complete != null)
                {
                    complete.Invoke(true);
                }
            };
            queue.Load();
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


        /// <summary>
        /// 根据tag获取所有bundle
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<BundleInfo> GetBundlesByTags(string tag)
        {
            List<BundleInfo> bundles = new List<BundleInfo>();
            foreach (string key in OKAsset.GetInstance().StorageBundlesInfo.Keys)
            {
                if (OKAsset.GetInstance().StorageBundlesInfo[key].bundleTag == tag)
                {
                    bundles.Add(OKAsset.GetInstance().StorageBundlesInfo[key]);
                }
            }
            return bundles;
        }
    }
}