using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OKAssets
{
    public class Util
    {
        /// <summary>
        /// 计算字符串的MD5值
        /// </summary>
        public static string md5(string source)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(source);
            byte[] md5Data = md5.ComputeHash(data, 0, data.Length);
            md5.Clear();

            string destString = "";
            for (int i = 0; i < md5Data.Length; i++)
            {
                destString += System.Convert.ToString(md5Data[i], 16).PadLeft(2, '0');
            }

            destString = destString.PadLeft(32, '0');
            return destString;
        }

        /// <summary>
        /// 计算文件的MD5值
        /// </summary>
        public static string md5file(string file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("md5file() fail, error:" + ex.Message);
            }
        }


        /// <summary>
        /// 取得数据存放目录
        /// </summary>
        public static string DataPath
        {
            get
            {
                string game = OKAssetsConst.okConfig.appName;
                if (OKAssetsConst.okConfig.loadModel == ResLoadMode.OnLineModel)
                {
                    return Application.persistentDataPath + "/" + game + "/";
                }

                if (OKAssetsConst.okConfig.loadModel == ResLoadMode.EditorModel)
                {
                    return Application.dataPath + "/" + OKAssetsConst.okConfig.ResFolderName + "/";
                }

                return "c:/" + game + "/";
            }
        }

        public static string GetPlatformName()
        {
#if UNITY_EDITOR
            return GetPlatformForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
#else
            return GetPlatformForAssetBundles(Application.platform);
#endif
        }


#if UNITY_EDITOR
        private static string GetPlatformForAssetBundles(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
#if UNITY_TVOS
                case BuildTarget.tvOS:
                    return "tvOS";
#endif
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
        }
#endif

#if !UNITY_EDITOR
        private static string GetPlatformForAssetBundles(RuntimePlatform target)
        {
            switch (target)
            {
                case RuntimePlatform.Android:
                    return "Android";
#if UNITY_TVOS
                case RuntimePlatform.tvOS:
                    return "tvOS";
#endif
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                    return "OSX";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
        }
#endif

        /// <summary>
        /// 网络可用
        /// </summary>
        public static bool NetAvailable
        {
            get { return Application.internetReachability != NetworkReachability.NotReachable; }
        }

        /// <summary>
        /// 是否是无线
        /// </summary>
        public static bool IsWifi
        {
            get { return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork; }
        }

        /**
         * 是否是Android 平台
         */
        public static bool IsAndroid()
        {
            return Application.platform == RuntimePlatform.Android;
        }

        /**
         * 是否是Ios平台
         */
        public static bool IsIos()
        {
            return Application.platform == RuntimePlatform.IPhonePlayer;
        }

        /// <summary>
        /// 应用程序内容路径
        /// </summary>
        public static string AppContentPath()
        {
            string path = string.Empty;
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    path = "jar:file://" + Application.dataPath + "!/assets/";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    path = Application.dataPath + "/Raw/";
                    break;
                default:
                    path = Application.dataPath + "/" + OKAssetsConst.okConfig.ResFolderName + "/";
                    break;
            }

            return path;
        }


        public static string GetAssetBundleStreamingAssetsPath()
        {
            return Application.streamingAssetsPath + "/" + OKAssetsConst.ASSETBUNDLE_FOLDER + "/" +
                   Util.GetPlatformName();
        }
        
        public static string GetBundlesInfoConfigStreamingAssetsPath()
        {
            return Path.Combine(GetAssetBundleStreamingAssetsPath(),
                OKAssetsConst.FILENAME_FILES_TXT);
        }

        public static string GetBundlesInfoConfigPersistentDataPath()
        {
            return Path.Combine(Util.DataPath, OKAssetsConst.FILENAME_FILES_TXT);
        }

        public static string GetBuildVersionConfigStreamingAssetsPath()
        {
            return Path.Combine(GetAssetBundleStreamingAssetsPath(),
                OKAssetsConst.FILENAME_BUILDVERSION_TXT);
        }

        public static string GetBuildVersionConfigPersistentDataPath()
        {
            return Path.Combine(Util.DataPath, OKAssetsConst.FILENAME_BUILDVERSION_TXT);
        }

        public static string GetBundlesTableConfigStreamingAssetsPath()
        {
            return Path.Combine(GetAssetBundleStreamingAssetsPath(),
                OKAssetsConst.FILENAME_BUNDLESTABLE_TXT);
        }

        public static string GetBundlesTableConfigPersistentDataPath()
        {
            return Path.Combine(Util.DataPath, OKAssetsConst.FILENAME_BUNDLESTABLE_TXT);
        }

    }
}