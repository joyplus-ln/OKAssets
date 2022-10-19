using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace OKAssets
{
    public class TexturePackerManager
    {
        public delegate void OnAssetPackCompleteDelegate(string tag);

        Dictionary<string, TexturePacker> _dicAllAssetPacker;
        Dictionary<string, List<string>> _dicAllPath;
        List<string> _listAllTagNotEnd;

        Transform _gameManagerTf;


        private static TexturePackerManager _instance;

        public static TexturePackerManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new TexturePackerManager();
            }

            return _instance;
        }

        public void Init()
        {
            _dicAllAssetPacker = new Dictionary<string, TexturePacker>();
            _dicAllPath = new Dictionary<string, List<string>>();
            _listAllTagNotEnd = new List<string>();

            _gameManagerTf = GameObject.Find("GameManager").transform;
        }

        /// 添加路径并打包
        public void AddPathAndComplete(string tag, string[] paths, OnAssetPackCompleteDelegate callBack = null,
            bool isUseCache = false)
        {
            List<string> files = GetPathList(tag);

            bool hasChange = false;
            foreach (string path in paths)
            {
                if (SavePathToListWithoutRepeat(files, path))
                {
                    hasChange = true;
                }
            }

            if (hasChange)
                CreateAssetPacker(tag, callBack, isUseCache);
        }

        /// 添加路径
        public void AddPath(string tag, string path)
        {
            List<string> files = GetPathList(tag);

            SavePathToListWithoutRepeat(files, path);
        }

        /// 移除路径
        public void RemovePath(string tag, string path)
        {
            if (_dicAllPath.ContainsKey(tag))
            {
                List<string> files = _dicAllPath[tag];
                if (files != null)
                    files.Remove(path);
            }
        }

        /// 清除路径
        public void ClearPath(string tag)
        {
            if (_dicAllPath.ContainsKey(tag))
            {
                List<string> files = _dicAllPath[tag];
                if (files != null)
                {
                    files.Clear();
                }
            }
        }

        void ClearAssetPackInfo(TexturePacker ap)
        {
            if (ap == null)
                return;

            string path = Application.persistentDataPath + "/TexturePacker/" + ap.cacheName;
            bool cacheExist = Directory.Exists(path);
            if (cacheExist)
                Directory.Delete(path, true);

            ap.Dispose();
        }

        /// 获取一个路径列表
        List<string> GetPathList(string tag)
        {
            if (_dicAllPath.ContainsKey(tag))
            {
                return _dicAllPath[tag];
            }

            List<string> mList = new List<string>();
            _dicAllPath.Add(tag, mList);

            return mList;
        }

        /// 存路径，同时去重；
        bool SavePathToListWithoutRepeat(List<string> list, string newPath)
        {
            newPath = Application.dataPath + "/" + newPath;

            //跳过重复
            foreach (string path in list)
            {
                if (path == newPath)
                    return false;
            }

            list.Add(newPath);
            return true;
        }

        /// 打包动态图集，只有Tag相同才会被打包
        public void CreateAssetPacker(string tag, OnAssetPackCompleteDelegate callBack = null, bool isUseCache = false)
        {
            if (string.IsNullOrEmpty(tag))
                return;

            // 判断是否在打包中
            foreach (string flag in _listAllTagNotEnd)
            {
                if (flag == tag)
                {
                    Debug.Log(" ...已经在打包图集中,请稍等....tag = " + tag);
                    return;
                }
            }

            // 获取所有的路径
            List<string> listPath = GetPathList(tag);

            // 开始打包图集
            TexturePacker ap;
            _dicAllAssetPacker.TryGetValue(tag, out ap);
            if (ap == null)
            {
                GameObject go = new GameObject("TexturePacker:" + tag);
                if (go == null)
                    return;

                go.transform.SetParent(_gameManagerTf);

                ap = go.AddComponent<TexturePacker>();

                _dicAllAssetPacker.Add(tag, ap);
            }
            else
            {
                ClearAssetPackInfo(ap);
            }

            ap.useCache = isUseCache;
            ap.cacheName = tag;
            ap.cacheVersion = 1;

            //设定参数
            _listAllTagNotEnd.Add(tag);

            ap.OnProcessCompleted.AddListener(delegate()
            {
                Debug.Log("动态图集" + tag + "更换完成。" + "数量：" + listPath.Count);

                OneTagPackEnd(tag);

                if (callBack != null)
                    callBack.Invoke(tag);
            });

            ap.AddTexturesToPack(listPath);

            //开始打图集；
            ap.Process();

            Debug.Log("开始打图集：" + tag + "数量：" + listPath.Count);
        }

        /// 一个图集打包完成
        void OneTagPackEnd(string tag)
        {
            _listAllTagNotEnd.Remove(tag);
            if (_listAllTagNotEnd.Count == 0)
            {
                //此时已经全部打包完成了；
                Debug.Log("全部图集打包完成！");
            }
        }

        /// 获取一张图片
        public Sprite GetSprite(string tag, string name)
        {
            if (_dicAllAssetPacker.ContainsKey(tag))
            {
                TexturePacker Pa = _dicAllAssetPacker[tag];
                return Pa.GetSprite(name);
            }

            return null;
        }
    }
}