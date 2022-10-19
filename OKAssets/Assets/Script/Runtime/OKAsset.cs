using System;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.U2D;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace OKAssets
{
    public class OKAsset : ITicker
    {
        public delegate void OnCompleteDelegate();

        public delegate void OnLoadItemCompleteDelegate();

        public delegate void OnLoadAssetCompleteDelegate(Object obj);

        public delegate void OnLoadAllAssetCompleteDelegate(Object[] objs);

        public delegate void OnLoadPrefabCompleteDelegate(GameObject gameObject);

        public delegate void OnLoadTexture2DCompleteDelegate(Texture2D texture2D);

        public delegate void OnLoadTextAssetCompleteDelegate(TextAsset texture2D);

        public delegate void OnLoadSpriteCompleteDelegate(Sprite sprite);

        public delegate void OnLoadAllSpriteInAtlasCompleteDelegate(AtlasInfo atlasInfo);

        public delegate void OnLoadAnimationClipCompleteDelegate(AnimationClip clip);

        public delegate void OnLoadScriptableObjectCompleteDelegate(ScriptableObject scriptableObject);

        public delegate void OnLoadMeshCompleteDelegate(Mesh mesh);

        public delegate void OnLoadMaterialCompleteDelegate(Material mat);

        public delegate void OnLoadAudioClipCompleteDelegate(AudioClip audio);

        public delegate void OnLoadVideoClipCompleteDelegate(VideoClip video);

        public delegate void OnLoadFontCompleteDelegate(Font font);

        private Dictionary<string, BundleInfo> _storageBundlesInfo = new Dictionary<string, BundleInfo>();

        private Dictionary<string, BundleInfo> _cdnBundlesInfo = new Dictionary<string, BundleInfo>();

        private AssetBundleManifest assetBundleManifest;

        class LoadingAssetBundleRequest
        {
            public string assetBundleName;
            public OnLoadItemCompleteDelegate onComplete;
        }

        private List<string> loadingAssetBundleNames;
        private Dictionary<string, List<LoadingAssetBundleRequest>> loadingBundleRequests;
        private Dictionary<string, string[]> dependenciesCache;

        private Dictionary<string, LoadedAssetBundle> loadedAssetBundles;

        //bundle和文件的对应表
        private Dictionary<string, string> bundleTable = new Dictionary<string, string>();
        private static OKAsset _instance;
        private bool initialized = false;
        private bool hotUpdate = false;

        public Dictionary<string, BundleInfo> StorageBundlesInfo
        {
            get { return _storageBundlesInfo; }
        }

        public static OKAsset GetInstance()
        {
            if (_instance == null)
            {
                _instance = new OKAsset();
                OKTimer.Inatance.Add(_instance);
            }

            return _instance;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if (initialized)
            {
                return;
            }

            loadingAssetBundleNames = new List<string>();
            loadingBundleRequests = new Dictionary<string, List<LoadingAssetBundleRequest>>();
            dependenciesCache = new Dictionary<string, string[]>();
            loadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
            bundleTable = OKResUtil.LoadBundlesTable();
            assetBundleManifest = OKResUtil.LoadManifest();
            initialized = true;
        }

        public void CheckUpdate(string URL, FileCompare.OnCompareCDNResult loadSuccessCallBack,
            Action loadErrorCallBack)
        {
            OKResUtil.CdnUrl = URL;
            string CDNVersionFileURL = OKResUtil.GetDefaultAssetBundlesCDNPath() + "/" +
                                       OKAssetsConst.FILENAME_BUILDVERSION_TXT;
            string CDNFileTableURL = OKResUtil.GetDefaultAssetBundlesCDNPath() + "/" + OKAssetsConst.FILENAME_FILES_TXT;

            void LoadSuccessful(bool needDownloadapp)
            {
                OKFileManager.CompareFiles(CDNFileTableURL, loadSuccessCallBack);
            }

            void LoadError()
            {
                loadErrorCallBack?.Invoke();
            }

            OKFileManager.CompareCDNBuildVersion(CDNVersionFileURL, LoadSuccessful, LoadError);
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="diffFilesInfo"></param>
        /// <param name="progress"></param>
        /// <param name="downLoadFailCallBack"></param>
        public void DownLoadDiffBundles(BundleInfo[] diffFilesInfo, OKFileManager.OnLoadQueueProgressDelegate progress,
            Action<List<BundleInfo>> downLoadFailCallBack)
        {
            OKResUtil.DownloadFilesByBundleInfo(diffFilesInfo, progress, downLoadFailCallBack);
        }


        /// <summary>
        /// 获取某个tag需要下载的大小
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public float GetBundleDownLoadSizeByTag(string tag)
        {
            float size = 0;
            BundleInfo[] totalTagBundle = OKFileManager.GetStorageBundleInfoListBySubpackage(tag);
            for (int i = 0; i < totalTagBundle.Length; i++)
            {
                BundleInfo bundleInfo = totalTagBundle[i];
                if (bundleInfo.location == BundleStorageLocation.CDN)
                {
                    size += bundleInfo.byteSize;
                }
            }

            return size;
        }

        /// <summary>
        /// 下载某个tag标记的所有资源
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="progress"></param>
        /// <param name="downLoadFailCallBack"></param>
        public void DownLoadBundleByTag(string tag, OKFileManager.OnLoadQueueProgressDelegate progress,
            Action<List<BundleInfo>> downLoadFailCallBack)
        {
            BundleInfo[] totalTagBundle = OKFileManager.GetStorageBundleInfoListBySubpackage(tag);
            List<BundleInfo> cdnBundleInfoList = new List<BundleInfo>();
            for (int i = 0; i < totalTagBundle.Length; i++)
            {
                BundleInfo bundleInfo = totalTagBundle[i];
                if (bundleInfo.location == BundleStorageLocation.CDN)
                {
                    cdnBundleInfoList.Add(bundleInfo);
                }
            }

            OKResUtil.DownloadFilesByBundleInfo(cdnBundleInfoList.ToArray(), progress, downLoadFailCallBack);
        }


        /// <summary>
        /// 加载shader资源和其变种
        /// </summary>
        private void LoadAndWarmUpShaderBundle()
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.OnLineModel)
            {
                ShaderVariantCollection svc = LoadShaderVariantCollection("Shader/CustomShaderVariants.shadervariants");
                if (svc != null && !svc.isWarmedUp)
                {
                    svc.WarmUp();
                }
            }
        }

        //加载一个bundle的依赖
        private void LoadDependencies(string assetBundleName)
        {
            if (!assetBundleManifest)
            {
                return;
            }

            string[] dependencies = GetDependencies(assetBundleName);
            if (dependencies == null)
            {
                return;
            }

            if (dependencies.Length == 0)
            {
                return;
            }

            for (int i = 0; i < dependencies.Length; i++)
            {
                //逐个加载依赖
                LoadAssetBundleInternal(dependencies[i]);
            }
        }

        private void LoadAssetBundleInternal(string assetBundleName,
            OnLoadItemCompleteDelegate onLoadItemComplete = null)
        {
            LoadedAssetBundle bundle = null;
            loadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            //已经加载过了，增加一个引用
            if (bundle != null)
            {
                bundle.AddReference();
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete();
                }

                return;
            }

            //正在加载中
            LoadingAssetBundleRequest request = new LoadingAssetBundleRequest();
            request.assetBundleName = assetBundleName;
            request.onComplete = onLoadItemComplete;
            List<LoadingAssetBundleRequest> requests = null;
            if (loadingBundleRequests.TryGetValue(assetBundleName, out requests))
            {
                requests.Add(request);
                return;
            }

            //添加到加载列表中
            requests = new List<LoadingAssetBundleRequest>();
            requests.Add(request);
            loadingBundleRequests.Add(assetBundleName, requests);
            loadingAssetBundleNames.Add(assetBundleName);
            //开始加载
            OKBaseLoader bundleLoader = GetAssetBundleLoader(assetBundleName);
            bundleLoader.Url = OKResUtil.GetAssetBundlePath(assetBundleName);
            bundleLoader.Name = assetBundleName;
            bundleLoader.IsAsync = true;
            bundleLoader.OnLoadComplete += delegate(OKBaseLoader l)
            {
                string curLoadedAssetBundleName = l.Name;
                loadedAssetBundles.TryGetValue(curLoadedAssetBundleName, out bundle);
                if (bundle == null)
                {
                    //添加到已经加载的列表中
                    loadedAssetBundles.Add(curLoadedAssetBundleName, new LoadedAssetBundle(l.AssetBundle, 0));
                    OKResUtil.WriteBundleToStorageAndUpdateBundleInfo(curLoadedAssetBundleName, _storageBundlesInfo,
                        l as OKAssetBundleLoader);
                }
            };
            bundleLoader.Load();
        }

        public void Update()
        {
            if (!initialized)
                return;

            for (int i = loadingAssetBundleNames.Count - 1; i >= 0; i--)
            {
                string assetBundleName = loadingAssetBundleNames[i];
                LoadedAssetBundle bundle = GetLoadedAssetBundle(assetBundleName);
                if (bundle != null)
                {
                    //loading 中的已经加载完成了
                    //从加载中的列表中清空，并且判断回调
                    List<LoadingAssetBundleRequest> loadingList = null;
                    if (loadingBundleRequests.TryGetValue(assetBundleName, out loadingList))
                    {
                        loadingBundleRequests.Remove(assetBundleName);
                        for (int index = 0; index < loadingList.Count; index++)
                        {
                            LoadingAssetBundleRequest request = loadingList[index];
                            if (loadedAssetBundles.ContainsKey(assetBundleName))
                            {
                                AddLoadedAssetBundleReference(assetBundleName);
                            }

                            if (request.onComplete != null)
                            {
                                request.onComplete();
                            }
                        }
                    }

                    loadingAssetBundleNames[i] = string.Empty;
                }
            }

            //删除不用的
            for (int i = loadingAssetBundleNames.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(loadingAssetBundleNames[i]))
                {
                    loadingAssetBundleNames.RemoveAt(i);
                }
            }
        }
        
        //判断一个assetbundle是否正在加载中
        private bool IsAssetBundleLoading(string assetBundleName)
        {
            if (loadingAssetBundleNames == null)
                return false;
            if (string.IsNullOrEmpty(assetBundleName))
                return false;
            for (int i = 0; i < loadingAssetBundleNames.Count; i++)
            {
                string loadingName = loadingAssetBundleNames[i];
                if (string.IsNullOrEmpty(loadingName))
                {
                    continue;
                }

                if (loadingName.Equals(assetBundleName))
                {
                    return true;
                }
            }

            return false;
        }

        private LoadedAssetBundle GetLoadedAssetBundle(string assetBundleName)
        {
            //先检查自身是否加载完成，因为如果自身都还没完成，那说明肯定没加载完成呢，可以减少对于下面依赖判断的次数
            LoadedAssetBundle loadedBundle = null;
            loadedAssetBundles.TryGetValue(assetBundleName, out loadedBundle);
            if (loadedBundle == null)
            {
                return null;
            }

            //检查依赖 如果有依赖，那么依次检查是否加载完
            string[] dependencies = GetDependencies(assetBundleName);
            if (dependencies != null && dependencies.Length > 0)
            {
                for (int depIndex = 0; depIndex < dependencies.Length; depIndex++)
                {
                    LoadedAssetBundle dependentBundle = null;
                    loadedAssetBundles.TryGetValue(dependencies[depIndex], out dependentBundle);
                    if (dependentBundle == null)
                    {
                        return null;
                    }
                }
            }

            return loadedBundle;
        }


        public AssetBundle LoadAssetBundle(string name)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
                return null;
            }

            //先加载依赖的内容 同步加载
            string[] deps = GetDependencies(name);
            OKBaseLoader depLoader;
            for (int i = 0; i < deps.Length; i++)
            {
                string dep = deps[i];
                LoadedAssetBundle depBundle = null;
                if (loadedAssetBundles.TryGetValue(dep, out depBundle))
                {
                    depBundle.AddReference();
                }
                else
                {
                    depLoader = GetAssetBundleLoader(dep);
                    depLoader.Url = OKResUtil.GetAssetBundlePath(dep);
                    depLoader.Name = dep;
                    depLoader.IsAsync = false;
                    depLoader.AutoDispose = false;
                    depLoader.Load();
                    loadedAssetBundles.Add(depLoader.Name, new LoadedAssetBundle(depLoader.AssetBundle, 1));
                    OKResUtil.WriteBundleToStorageAndUpdateBundleInfo(dep, _storageBundlesInfo,
                        depLoader as OKAssetBundleLoader);
                    depLoader.Dispose();
                }
            }

            LoadedAssetBundle bundle = null;
            if (loadedAssetBundles.TryGetValue(name, out bundle))
            {
                bundle.AddReference();
                return bundle.assetBundle;
            }
            else
            {
                OKBaseLoader loader = GetAssetBundleLoader(name);
                loader.Url = OKResUtil.GetAssetBundlePath(name);
                loader.Name = name;
                loader.IsAsync = false;
                loader.AutoDispose = false;
                loader.Load();
                AssetBundle asset = loader.AssetBundle;
                loadedAssetBundles.Add(name, new LoadedAssetBundle(asset, 1));
                OKResUtil.WriteBundleToStorageAndUpdateBundleInfo(name, _storageBundlesInfo,
                    loader as OKAssetBundleLoader);
                loader.Dispose();
                return asset;
            }
        }

        public void LoadAssetBundleAsync(string name, OnLoadItemCompleteDelegate onLoadItemComplete = null)
        {
            LoadDependencies(name);
            LoadAssetBundleInternal(name, onLoadItemComplete);
        }

        public void UnloadAssetBundle(string assetPath)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
                return;
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string abName = GetAssetBundleNameByAssetPath(assetPath);
            if (string.IsNullOrEmpty(abName))
            {
                return;
            }

            UnloadAssetBundleInternal(abName);
            UnloadDependencies(abName);
        }

        private void UnloadAssetBundleInternal(string assetBundleName)
        {
            LoadedAssetBundle bundle = GetLoadedAssetBundle(assetBundleName);
            if (bundle == null)
                return;

            if (bundle.DeleteReference() <= 0)
            {
                if (IsAssetBundleLoading(assetBundleName))
                {
                    return; //如果当前AB处于Async Loading过程中，卸载会崩溃，只减去引用计数即可
                }

                bundle.Unload();
                loadedAssetBundles.Remove(assetBundleName);
                if (assetBundleName.StartsWith("ui_texture_") || assetBundleName.StartsWith("puzzle_texture"))
                {
                    //如果这个卸载的bundle是ui_texture_开头的，那么移除AtlasManger里的
                    AtlasManager.GetInstance().RemoveAtlas(assetBundleName);
                }
            }
        }

        private void UnloadDependencies(string assetBundleName)
        {
            string[] dependencies = GetDependencies(assetBundleName);
            if (dependencies == null)
            {
                return;
            }

            for (int i = 0; i < dependencies.Length; i++)
            {
                UnloadAssetBundleInternal(dependencies[i]);
            }
        }

        public void LoadAssetAsync(string assetPath, OnLoadAssetCompleteDelegate onLoadItemComplete = null,
            bool autoLoadDependencies = true)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
#if UNITY_EDITOR
                UnityEngine.Object target =
                    AssetDatabase.LoadMainAssetAtPath(string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                if (target != null)
                {
                    if (onLoadItemComplete != null)
                    {
                        onLoadItemComplete(target);
                    }

                    return;
                }

                Debug.LogWarning("资源无法通过编辑器模式加载:" + string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                return;
#endif
            }
            else
            {
                string abName = GetAssetBundleNameByAssetPath(assetPath);
                if (string.IsNullOrEmpty(abName))
                {
                    return;
                }

                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                LoadAssetBundleAsync(abName, delegate()
                {
                    LoadedAssetBundle loadedBundle = null;
                    if (loadedAssetBundles.TryGetValue(abName, out loadedBundle))
                    {
                        AssetLoader loader = new AssetLoader();
                        loader.assetBundle = loadedBundle.assetBundle;
                        loader.assetName = assetName;
                        loader.OnLoadComplete += delegate(OKBaseLoader l)
                        {
                            if (onLoadItemComplete != null)
                            {
                                onLoadItemComplete(l.Content as UnityEngine.Object);
                            }
                        };

                        loader.Load();
                    }
                });
            }
        }


        public Object LoadAsset(string assetPath, bool autoLoadDependencies = true)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
#if UNITY_EDITOR
                UnityEngine.Object target =
                    AssetDatabase.LoadMainAssetAtPath(string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                if (target != null)
                {
                    return target;
                }

                Debug.LogWarning("资源无法通过编辑器模式加载:" + string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                return null;
#else
			return null;
#endif
            }
            else
            {
                string abName = GetAssetBundleNameByAssetPath(assetPath);
                if (string.IsNullOrEmpty(abName))
                {
                    return null;
                }

                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                AssetBundle ab = LoadAssetBundle(abName);
                if (ab == null)
                {
                    return null;
                }

                AssetLoader loader = new AssetLoader();
                loader.isAsync = false;
                loader.assetBundle = ab;
                loader.assetName = assetName;
                UnityEngine.Object con = loader.GetAsset<UnityEngine.Object>();
                loader.Dispose();
                return con;
            }
        }

        public void LoadScene(string assetPath, bool autoLoadDependencies = true)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
#if UNITY_EDITOR
                UnityEngine.Object target =
                    AssetDatabase.LoadMainAssetAtPath(string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                Debug.LogWarning("资源无法通过编辑器模式加载:" + string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
#else
#endif
            }
            else
            {
                string abName = GetAssetBundleNameByAssetPath(assetPath);
                if (string.IsNullOrEmpty(abName))
                {
                    return;
                }

                LoadAssetBundle(abName);
            }
        }

        public void LoadAllAssetAsync(string assetPath, OnLoadAllAssetCompleteDelegate onLoadItemComplete = null,
            bool autoLoadDependencies = true)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
#if UNITY_EDITOR
                UnityEngine.Object[] targets =
                    AssetDatabase.LoadAllAssetsAtPath(string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                if (targets != null)
                {
                    if (onLoadItemComplete != null)
                    {
                        onLoadItemComplete(targets);
                    }

                    return;
                }

                Debug.LogWarning("资源无法通过编辑器模式加载:" + string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                return;
#endif
            }
            else
            {
                string abName = GetAssetBundleNameByAssetPath(assetPath);

                if (string.IsNullOrEmpty(abName))
                {
                    return;
                }

                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                LoadAssetBundleAsync(abName, delegate()
                {
                    LoadedAssetBundle loadedBundle = null;
                    if (loadedAssetBundles.TryGetValue(abName, out loadedBundle))
                    {
                        AllAssetLoader loader = new AllAssetLoader();
                        loader.assetBundle = loadedBundle.assetBundle;
                        loader.OnLoadComplete += delegate(OKBaseLoader l)
                        {
                            if (onLoadItemComplete != null)
                            {
                                onLoadItemComplete(loader.GetAllAssets<UnityEngine.Object>());
                            }
                        };

                        loader.Load();
                    }
                });
            }
        }


        public Object[] LoadAllAsset(string assetPath, bool autoLoadDependencies = true)
        {
            if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
            {
#if UNITY_EDITOR
                UnityEngine.Object[] targets =
                    AssetDatabase.LoadAllAssetsAtPath(string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                if (targets != null)
                {
                    return targets;
                }

                Debug.LogWarning("资源无法通过编辑器模式加载:" + string.Concat(OKAssetsConst.ASSET_PATH_PREFIX, assetPath));
                return null;
#else
			return null;
#endif
            }
            else
            {
                string abName = GetAssetBundleNameByAssetPath(assetPath);

                if (string.IsNullOrEmpty(abName))
                {
                    return null;
                }

                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                AssetBundle ab = LoadAssetBundle(abName);
                if (ab == null)
                {
                    return null;
                }

                AllAssetLoader loader = new AllAssetLoader();
                loader.isAsync = false;
                loader.assetBundle = ab;
                UnityEngine.Object[] objArray = loader.GetAllAssets<UnityEngine.Object>();
                loader.Dispose();
                return objArray;
            }
        }

        public ShaderVariantCollection LoadShaderVariantCollection(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            return obj as ShaderVariantCollection;
        }

        public void LoadPrefabAsync(string assetPath, OnLoadPrefabCompleteDelegate onLoadItemComplete = null,
            bool autoLoadDependencies = true)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as GameObject);
                }
            });
        }

        public GameObject LoadPrefab(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            return obj as GameObject;
        }

        public Sprite LoadSprite(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            if (obj)
            {
                Sprite sp = obj as Sprite;
                if (sp == null)
                {
                    Texture2D tex = obj as Texture2D;
                    if (tex)
                    {
                        sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }

                return sp;
            }

            return null;
        }


        public Texture LoadTexture(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            if (obj)
            {
                Sprite sp = obj as Sprite;
                if (sp == null)
                {
                    Texture2D tex = obj as Texture2D;
                    if (tex)
                    {
                        return tex;
                    }
                }
            }

            return null;
        }

        public Sprite LoadSpriteWithBorder(string assetPath, Vector4 border)
        {
            Object obj = LoadAsset(assetPath);
            if (obj)
            {
                Sprite sp = obj as Sprite;
                if (sp == null)
                {
                    Texture2D tex = obj as Texture2D;
                    if (tex)
                    {
                        sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 1,
                            SpriteMeshType.FullRect, border);
                    }
                }

                return sp;
            }

            return null;
        }

        public void LoadSpriteAsync(string assetPath, OnLoadSpriteCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    Sprite sp = obj as Sprite;
                    if (sp == null)
                    {
                        Texture2D tex = obj as Texture2D;
                        if (tex)
                        {
                            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }

                    onLoadItemComplete(sp);
                }
            });
        }

        public void LoadSpriteAsyncWithBorder(string assetPath, Vector4 border,
            OnLoadSpriteCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    Sprite sp = obj as Sprite;
                    if (sp == null)
                    {
                        Texture2D tex = obj as Texture2D;
                        if (tex)
                        {
                            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100,
                                1,
                                SpriteMeshType.FullRect, border);
                        }
                    }

                    onLoadItemComplete(sp);
                }
            });
        }

        public Sprite LoadSpriteInAtlas(string assetPath, string atlasPath, string spriteName)
        {
            AtlasInfo atlasInfo = LoadAllSpriteInAtlas(atlasPath);
            spriteName = Path.GetFileNameWithoutExtension(spriteName); //spriteName不需要扩展名
            return atlasInfo.GetSprite(spriteName);
        }

        public void LoadSpriteInAtlasAsync(string assetPath, string atlasPath, string spriteName,
            OnLoadSpriteCompleteDelegate onLoadItemComplete = null)
        {
            LoadAllSpriteInAtlasAsync(atlasPath, delegate(AtlasInfo atlasInfo)
            {
                spriteName = Path.GetFileNameWithoutExtension(spriteName); //spriteName不需要扩展名
                Sprite sp = atlasInfo.GetSprite(spriteName);
                if (sp != null)
                {
                    if (onLoadItemComplete != null)
                    {
                        onLoadItemComplete(sp);
                    }
                }
            });
        }

        public AtlasInfo LoadAllSpriteInAtlas(string assetPath)
        {
            AtlasInfo atlas = AtlasManager.GetInstance().GetAtlas(assetPath);
            if (atlas != null)
            {
                //增加引用
                AddLoadedAssetBundleReference(assetPath);
                return atlas;
            }

            SpriteAtlas _atlas = LoadAsset(assetPath) as SpriteAtlas;
            if (_atlas != null)
            {
                Sprite[] sps = new Sprite[_atlas.spriteCount];
                if (_atlas.GetSprites(sps) > 0)
                {
                    atlas = new AtlasInfo();
                    atlas.AddSprites(sps);
                    AtlasManager.GetInstance().AddAtlas(assetPath, atlas);
                }
            }

            return atlas;
        }

        public void LoadAllSpriteInAtlasAsync(string assetPath,
            OnLoadAllSpriteInAtlasCompleteDelegate onLoadItemComplete = null)
        {
            AtlasInfo atlas = AtlasManager.GetInstance().GetAtlas(assetPath);
            if (atlas != null)
            {
                //增加引用
                AddLoadedAssetBundleReference(assetPath);
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(atlas);
                }

                return;
            }

            LoadAssetAsync(assetPath, (asset) =>
            {
                SpriteAtlas _atlas = asset as SpriteAtlas;
                if (_atlas != null)
                {
                    Sprite[] sps = new Sprite[_atlas.spriteCount];
                    if (_atlas.GetSprites(sps) > 0)
                    {
                        atlas = new AtlasInfo();
                        atlas.AddSprites(sps);
                        AtlasManager.GetInstance().AddAtlas(assetPath, atlas);
                        if (onLoadItemComplete != null)
                        {
                            onLoadItemComplete(atlas);
                        }
                    }
                }
            });
        }

        public void LoadTexture2DAsync(string assetPath, OnLoadTexture2DCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as Texture2D);
                }
            });
        }

        public Texture2D LoadTexture2D(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            if (obj)
            {
                return obj as Texture2D;
            }

            return null;
        }

        public void LoadTextAssetAsync(string assetPath, OnLoadTextAssetCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as TextAsset);
                }
            });
        }

        public TextAsset LoadTextAsset(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            if (obj)
            {
                return obj as TextAsset;
            }

            return null;
        }

        // public SkeletonDataAsset LoadSpine(string assetPath)
        // {
        //     Object obj = LoadAsset(assetPath);
        //     if (obj)
        //     {
        //         return obj as SkeletonDataAsset;
        //     }
        //     return null;
        // }
        public void LoadAnimationClipAsync(string assetPath,
            OnLoadAnimationClipCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as AnimationClip);
                }
            });
        }

        public AnimationClip LoadAnimationClip(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            return obj as AnimationClip;
        }

        public ScriptableObject LoadScriptableObject(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            if (obj)
            {
                return obj as ScriptableObject;
            }

            return null;
        }

        public void LoadScriptableObjectAsync(string assetPath,
            OnLoadScriptableObjectCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as ScriptableObject);
                }
            });
        }

        public void LoadMeshAsync(string assetPath, OnLoadMeshCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as Mesh);
                }
            });
        }

        public void LoadMaterialAsync(string assetPath, OnLoadMaterialCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as Material);
                }
            });
        }

        public Material LoadMaterial(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            return obj as Material;
        }

        public void LoadAudioClipAsync(string assetPath, OnLoadAudioClipCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as AudioClip);
                }
            });
        }

        public void LoadVideoClipAsync(string assetPath, OnLoadVideoClipCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as VideoClip);
                }
            });
        }

        public AudioClip LoadAudioClip(string assetPath)
        {
            Object obj = LoadAsset(assetPath);
            return obj as AudioClip;
        }

        public void LoadFontAsync(string assetPath, OnLoadFontCompleteDelegate onLoadItemComplete = null)
        {
            LoadAssetAsync(assetPath, delegate(Object obj)
            {
                if (onLoadItemComplete != null)
                {
                    onLoadItemComplete(obj as Font);
                }
            });
        }

        private string[] GetDependencies(string assetBundleName)
        {
            if (assetBundleManifest == null)
            {
                return null;
            }

            assetBundleName += OKAssetsConst.VARIANT;

            string[] dependencies = null;
            //判断从依赖缓存中取出依赖列表
            if (!dependenciesCache.TryGetValue(assetBundleName, out dependencies))
            {
                dependencies = assetBundleManifest.GetAllDependencies(assetBundleName);
                for (int i = 0; i < dependencies.Length; i++)
                {
                    dependencies[i] = dependencies[i].Replace(OKAssetsConst.VARIANT, "");
                }

                dependenciesCache.Add(assetBundleName, dependencies);
            }

            return dependencies;
        }


        private string GetAssetBundleNameByAssetPath(string path)
        {
            string abName;
            if (!bundleTable.TryGetValue(path, out abName))
            {
                Debug.LogWarning(string.Format("通过{0}查找的AssetBundle不存在", path));
            }

            return abName;
        }


        public OKBaseLoader GetAssetBundleLoader(string assetBundleName)
        {
            assetBundleName += OKAssetsConst.VARIANT;
            BundleInfo bundleInfo = OKResUtil.GetBundleInfo(assetBundleName);
            if (bundleInfo != null)
            {
                if (bundleInfo.location == BundleStorageLocation.CDN)
                {
                    return new OKAssetBundleLoader();
                }

                return new AssetBundleFromFileLoader();
            }

            return new AssetBundleFromFileLoader();
        }


        public void AddLoadedAssetBundleReference(string assetBundleName)
        {
            //增加引用
            LoadedAssetBundle bundle = null;
            loadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            //已经加载过了，增加一个引用
            if (bundle != null)
            {
                bundle.AddReference();
            }
        }
    }
}