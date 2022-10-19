using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace OKAssets.Editor
{
    public class OKBundlesInitScript
    {
        [MenuItem("OKAssets/CreatConfig")]
        public static void InitConfig()
        {
            CreatOkBundleConfig();
            CreatOkAssetTreeData();
            CreatOKBundleVersionData();
            CreatOKBundleTagData();
        }

        private static void CreatOkBundleConfig()
        {
            string path = Application.dataPath + "/Resources";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string configPath = Application.dataPath + $"/Resources/{OKAssetsConst.CONFIGNAME}";
            if (File.Exists(configPath))
            {
                return;
            }

            OKEditorUtil.CreatScriptObject<OKAssetsConfig>($"Assets/Resources/{OKAssetsConst.CONFIGNAME}");
        }

        [MenuItem("OKAssets/BuildAssetBundle")]
        public static void BuildBundle()
        {
            AssetbundleBuildScript.HybridBuildAssetBundles(BuildAssetBundleOptions.DeterministicAssetBundle |
                                                           BuildAssetBundleOptions.ChunkBasedCompression);
        }

        public static void CreatOkAssetTreeData(bool reCreat = false, OKTreeAsset dataScript = null)
        {
            string configPath = Application.dataPath.Replace("/Assets", "") + $"/{OKAssetsConst.OKAssetBundleData}";
            if (File.Exists(configPath))
            {
                if (reCreat)
                {
                    AssetDatabase.DeleteAsset(OKAssetsConst.OKAssetBundleData);
                    AssetDatabase.Refresh();
                }
                else
                {
                    return;
                }
            }

            OKEditorUtil.CreatScriptObject($"{OKAssetsConst.OKAssetBundleData}", dataScript);
        }

        public static void CreatOKBundleVersionData()
        {
            string configPath = Application.dataPath +
                                $"/{Util.GetPlatformName()}_{OKAssetsConst.OKAssetBundleVersionData}";
            if (File.Exists(configPath))
            {
                return;
            }

            OKEditorUtil.CreatScriptObject<OKBundlesBuildVersion>(
                $"Assets/{Util.GetPlatformName()}_{OKAssetsConst.OKAssetBundleVersionData}");
        }

        public static void CreatOKBundleTagData()
        {
            string configPath = Application.dataPath.Replace("/Assets", "") + $"/{OKAssetsConst.OKAssetBundleData}";
            if (File.Exists(configPath))
            {
                return;
            }

            OKEditorUtil.CreatScriptObject<OKBundlesBuildTag>($"Assets/{OKAssetsConst.OKAssetBundleTagData}");
        }
    }
}