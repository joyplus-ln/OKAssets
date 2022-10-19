using UnityEngine;

namespace OKAssets.Editor
{
    public class OkBundleEditorConst
    {
        public static Color[] BundlePackageColor = new Color[]
        {
            Color.white,
            Color.cyan,
            Color.green,
            Color.yellow,
            Color.magenta,
        };

        public enum BundlePackageType
        {
            NONE, //无
            SINGLE, //目录下每个文件标记为一个单独的Bundle
            FOLDER_ALL_IN_ONE, //目录下所有文件标记为同一个Bundle
            FOLDER_ALL_IN_ONE_RECURSIVELY, //目录下所有文件标记为同一个Bundle（递归）
            SINGLE_EXTS, //目录下每个文件(带有扩展名)标记为一个单独的Bundle
        }

        /// <summary>
        /// 资源保存的位置
        /// </summary>
        public enum BundleLocation
        {
            Local = 0, //无
            OnLine = 1
        }

        public static string[] BundleLocationName = new string[]
        {
            "Local",
            "OnLine",
        };

        public static string[] BundlePackageOptions = new string[]
        {
            "不打包",
            "每个文件标记为一个单独的Bundle",
            "所有文件标记为同一个Bundle",
            "所有文件标记为同一个Bundle（递归）",
            "每个文件(带有扩展名)标记为一个单独的Bundle",
        };

        public static string GetFolderBundleNameForEditor(string path, OKBundlesTreeElement item)
        {
            if (path.IndexOf('/') == 0)
            {
                path = path.Substring(1);
            }

            string result = "";
            switch ((BundlePackageType)item.folderBundleType)
            {
                case BundlePackageType.NONE:
                    //有可能是递归的 要查找下他的父级
                    OKBundlesTreeElement parent = GetParentUnNoneBundleType(item);
                    if (parent != null)
                    {
                        result = GetFolderBundleNameForEditor(parent.path, parent);
                    }

                    break;
                case BundlePackageType.SINGLE:
                    result = path.Replace('/', '_');
                    result += "_{filename}";
                    break;
                case BundlePackageType.SINGLE_EXTS:
                    result = path.Replace('/', '_');
                    result += "_{filename}_{fileextension}";
                    break;
                case BundlePackageType.FOLDER_ALL_IN_ONE:
                    result = path.Replace('/', '_');
                    break;
                case BundlePackageType.FOLDER_ALL_IN_ONE_RECURSIVELY:
                    result = path.Replace('/', '_');
                    break;
            }

            result = result.ToLower();
            return result;
        }

        public static OKBundlesTreeElement GetParentUnNoneBundleType(OKBundlesTreeElement item)
        {
            if (item.folderBundleType == (int)BundlePackageType.FOLDER_ALL_IN_ONE_RECURSIVELY)
            {
                return item;
            }

            if (item.parent != null)
            {
                return GetParentUnNoneBundleType((OKBundlesTreeElement)item.parent);
            }

            return null;
        }
    }
}