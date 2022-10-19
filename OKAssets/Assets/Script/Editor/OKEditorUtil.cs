using System.IO;
using UnityEditor;
using UnityEngine;

namespace OKAssets.Editor
{
    public class OKEditorUtil
    {

        public static T CreatScriptObject<T>(string path,T t = null) where T : ScriptableObject
        {
            if (t == null)
            {
                t =  ScriptableObject.CreateInstance<T>();
            }
            AssetDatabase.CreateAsset(t, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return t;
        }
        public static string GetActivePlatformBuildVersionFileName()
        {
            return $"OKAssetBuild{EditorUserBuildSettings.activeBuildTarget}.asset";
        }
        private static OKBundlesBuildVersion LoadBundleVersionData()
        {
            
            string path = $"Assets/{GetActivePlatformBuildVersionFileName()}";
            if (!File.Exists(path))
            {
                
            }
            OKBundlesBuildVersion buildVersions = AssetDatabase.LoadMainAssetAtPath(path) as
                OKBundlesBuildVersion;
            return buildVersions;
        }
    }
}