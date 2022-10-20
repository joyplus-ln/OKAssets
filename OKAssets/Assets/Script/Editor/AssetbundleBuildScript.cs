using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OKAssets.Editor
{
    public class AssetbundleBuildScript
    {
        static string CreateStreamDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            if (!File.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return dir;
        }

        public static void HybridBuildAssetBundles(BuildAssetBundleOptions buildOptions)
        {
            List<string> paths = new List<string>();
            List<string> files = new List<string>();
            Caching.ClearCache();
            string currCSMD5 = CalcCSharpMD5(paths, files);
            OKBundlesInitScript.CreatOKBundleVersionData();
            string configPath = $"Assets/{Util.GetPlatformName()}_{OKAssetsConst.OKAssetBundleVersionData}";
            OKBundlesBuildVersion buildVersions =
                AssetDatabase.LoadAssetAtPath<OKBundlesBuildVersion>(configPath);

            Version version = new Version(buildVersions.bundleVersion);
            int build = version.Build;
            if (currCSMD5.Equals(buildVersions.csharpMD5) == false)
                build++;
            //版本号在项目中的定义如下
            //Major表示游戏内容版本，一般游戏不同资料片这种大的内容更新情况下这个会变化								--手动改
            //Minor表示不同渠道，每个渠道会有一个固定的Minor，同一个渠道的Minor不会变化							--手动改
            //Build表示涉及到C#或必须更新整个安装包的版本,如果Build发生变化，那么需要重新下载安装包重新安装			--打包脚本自动改
            //Revision表示每次构建游戏的版本																	--打包脚本自动改
            //注：以上4个版本号如果只有Revision发生变化表示可以热更新
            Version newVersion = new Version(version.Major, version.Minor, build, version.Revision + 1);
            buildVersions.bundleVersion = newVersion.ToString();
            buildVersions.csharpMD5 = currCSMD5;
            EditorUtility.SetDirty(buildVersions);
            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
            //要构建的Bundle列表
            List<AssetBundleBuild> abbList = new List<AssetBundleBuild>();

            CreateStreamDir(Application.streamingAssetsPath + "/" + OKAssetsConst.ASSETBUNDLE_FOLDER + "/" +
                            Util.GetPlatformName());
            Caching.ClearCache();

            AssetDatabase.Refresh();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            string output = string.Format("{0}/{1}/{2}/", Application.dataPath.Replace("/Assets", ""),
                OKAssetsConst.ASSETBUNDLE_FOLDER,
                Util.GetPlatformName());
            CreateStreamDir(output);


            AssetDatabase.Refresh();
            OKTreeAsset mOkTreeAsset =
                AssetDatabase.LoadMainAssetAtPath(OKAssetsConst.OKAssetBundleData) as OKTreeAsset;
            //Initialize tree
            OKBaseTreeElementUtility.ListToTree(mOkTreeAsset.treeElements);
            Dictionary<string, List<string>> abDict = new Dictionary<string, List<string>>();
            Dictionary<string, OKBundlesTreeElement> abItemDictByPath =
                new Dictionary<string, OKBundlesTreeElement>();
            foreach (OKBundlesTreeElement item in mOkTreeAsset.treeElements)
            {
                if (item.depth == -1)
                {
                    continue;
                }

                string fullPath = Application.dataPath + $"/{OKAssetsConst.okConfig.ResFolderName}" + item.path;
                fullPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');

                DirectoryInfo folder = new DirectoryInfo(fullPath);
                FileInfo[] totalFiles = folder.GetFiles();
                foreach (FileInfo file in totalFiles)
                {
                    if (file.FullName.EndsWith(".manifest") || file.FullName.EndsWith(".meta") ||
                        file.FullName.Contains(".DS_Store") || file.FullName.Contains(".vscode") ||
                        file.FullName.Contains(".svn") || file.FullName.Contains(".git") ||
                        file.FullName.Contains(".idea") || file.FullName.Contains(".js.map"))
                    {
                        continue;
                    }

                    //图集的话只打图集本身，不对图片打单独的bundle了
                    if (file.FullName.Contains(OKAssetsConst.ATLASTAG) && !file.FullName.EndsWith(".spriteatlas"))
                    {
                        continue;
                    }

                    string abname = OkBundleEditorConst.GetFolderBundleNameForEditor(item.path, item);
                    abname = abname.Replace("{filename}", Path.GetFileNameWithoutExtension(file.Name).ToLower());
                    abname = abname.Replace("{fileextension}", Path.GetExtension(file.Name).Replace(".", "").ToLower());
                    List<string> assetList;
                    if (!abDict.TryGetValue(abname, out assetList))
                    {
                        assetList = new List<string>();
                        abDict[abname] = assetList;
                        abItemDictByPath.Add(abname, item);
                    }

                    string assetPath = file.FullName;
                    assetPath = assetPath.Replace(Path.DirectorySeparatorChar, '/');
                    assetPath = assetPath.Substring(assetPath.IndexOf(Application.dataPath) +
                                                    Application.dataPath.Length);
                    assetPath = "Assets" + assetPath;
                    assetList.Add(assetPath);
                }
            }

            foreach (string key in abDict.Keys)
            {
                AssetBundleBuild abb = new AssetBundleBuild();
                abb.assetBundleName = key;
                abb.assetBundleVariant = OKAssetsConst.VARIANT;
                abb.assetNames = abDict[key].ToArray();
                abbList.Add(abb);
            }

            BuildPipeline.BuildAssetBundles(output, abbList.ToArray(), buildOptions,
                EditorUserBuildSettings.activeBuildTarget);
            BuildBundleIndex(output, abDict);
            Debug.Log("BuildVersion:" + buildVersions.bundleVersion);
            BuildVersionsFile(output, buildVersions.bundleVersion);
            BuildFileIndex(output, paths, files, abItemDictByPath);
            //MoveManifestFilesToTempFolder();
            MoveOnLineBundleOut(abItemDictByPath, paths, files);
            AssetDatabase.Refresh();
        }

        static void BuildVersionsFile(string resPath, string verionStr)
        {
            string newFilePath = resPath + "buildversion.txt";
            if (File.Exists(newFilePath)) File.Delete(newFilePath);
            FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(verionStr);
            sw.Close();
            fs.Close();
        }

        static void BuildBundleIndex(string resPath, Dictionary<string, List<string>> abDict)
        {
            string newFilePath = resPath + "bundles_table.txt";
            if (File.Exists(newFilePath)) File.Delete(newFilePath);

            string prefix = "Assets/Bundles/";
            FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs);
            foreach (string key in abDict.Keys)
            {
                foreach (string path in abDict[key])
                {
                    sw.WriteLine(path.Substring(path.IndexOf(prefix) + prefix.Length) + "|" + key);
                }
            }

            sw.Close();
            fs.Close();
        }


        static void BuildFileIndex(string resPath, List<string> paths, List<string> files,
            Dictionary<string, OKBundlesTreeElement> abItemDictByPath)
        {
            string output = string.Format("{0}/{1}/{2}/", Application.streamingAssetsPath,
                OKAssetsConst.ASSETBUNDLE_FOLDER,
                Util.GetPlatformName());
            string newFilePath = resPath + "bundleFiles.txt";
            string streamingFilePath = output + "/bundleFiles.txt";
            string hashExName = "_hash";
            if (File.Exists(newFilePath)) File.Delete(newFilePath);
            if (File.Exists(streamingFilePath)) File.Delete(streamingFilePath);
            paths.Clear();
            files.Clear();
            Recursive(resPath,files, paths);

            FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs);

            FileStream sfs = new FileStream(streamingFilePath, FileMode.CreateNew);
            StreamWriter ssw = new StreamWriter(sfs);

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];
                if (file.EndsWith(".manifest") || file.EndsWith(".meta") || file.Contains(".DS_Store") ||
                    file.Contains(".vscode") || file.Contains(".svn") || file.Contains(".git") ||
                    file.Contains(".idea")) continue;
                FileInfo fileInfo = new FileInfo(file);
                BundleInfo fbi = new BundleInfo();
                fbi.name = file.Replace(resPath, string.Empty);
                fbi.nameWithHash = fbi.name;
                fbi.byteSize = fileInfo.Length;
                fbi.crcOrMD5Hash = Util.md5file(file);
                //这样的话，GResManager加载的时候就能知道，这个文件本地目前没有
                //如果游戏过程中下载过，那么会改成STORAGE
                fbi.location = BundleStorageLocation.STREAMINGASSETS;
                fbi.bundleTag = OKAssetsConst.Basic;

                if (fileInfo.Name.Contains(".ab"))
                {
                    int index = fileInfo.Name.LastIndexOf('.');
                    string fileExten = fileInfo.Name.Substring(index + 1, fileInfo.Name.Length - index - 1);
                    string nameWithHash =
                        $"{fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf('.'))}{hashExName}{fbi.crcOrMD5Hash}";
                    fbi.nameWithHash = $"{nameWithHash}.{fileExten}";
                    File.Copy(file,
                        $"{output}/{nameWithHash}.{fileExten}", true);
                }
                else
                {
                    File.Copy(file,
                        $"{output}/{fileInfo.Name}", true);
                }

                string bundleNameLower = Path.GetFileNameWithoutExtension(fileInfo.Name.ToLower());
                if (abItemDictByPath.ContainsKey(bundleNameLower))
                {
                    OKBundlesTreeElement hbte = abItemDictByPath[bundleNameLower];
                    if (hbte != null)
                    {
                        fbi.bundleTag = hbte.bundleTag;
                        fbi.loactionType = (BundleLocation)hbte.Location;
                        if (fbi.loactionType == BundleLocation.Local)
                        {
                            fbi.location = BundleStorageLocation.STREAMINGASSETS;
                        }
                        else
                        {
                            fbi.location = BundleStorageLocation.CDN;
                        }
                    }
                }

                sw.WriteLine(fbi.Output());
                ssw.WriteLine(fbi.Output());
            }

            sw.Close();
            fs.Close();
            ssw.Close();
            sfs.Close();
        }

        static void MoveManifestFilesToTempFolder()
        {
            string output = string.Format("{0}/{1}/{2}/", Application.streamingAssetsPath,
                OKAssetsConst.ASSETBUNDLE_FOLDER,
                Util.GetPlatformName());
            string tempFolder = Path.Combine(Application.dataPath.Replace("Assets", ""), "TempManifests");
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }

            Directory.CreateDirectory(tempFolder);
            //把.manifest的文件从output文件夹移动到外面的临时文件夹中
            string[] manifestFilesPath = Directory.GetFiles(output, "*.manifest");
            foreach (string mffp in manifestFilesPath)
            {
                Directory.Move(mffp, Path.Combine(tempFolder, Path.GetFileName(mffp)));
            }
        }

        static void MoveOnLineBundleOut(Dictionary<string, OKBundlesTreeElement> abItemDictByPath,
            List<string> paths, List<string> files)
        {
            string tempFolder = Path.Combine(Application.dataPath.Replace("Assets", ""),
                "OnLineBundle/" + Util.GetPlatformName());
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }

            Directory.CreateDirectory(tempFolder);
            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];
                if (file.EndsWith(".manifest") || file.EndsWith(".meta") || file.Contains(".DS_Store") ||
                    file.Contains(".vscode") || file.Contains(".svn") || file.Contains(".git") ||
                    file.Contains(".idea")) continue;
                FileInfo fileInfo = new FileInfo(file);
                string bundleNameLower = Path.GetFileNameWithoutExtension(fileInfo.Name.ToLower());
                if (abItemDictByPath.ContainsKey(bundleNameLower))
                {
                    OKBundlesTreeElement hbte = abItemDictByPath[bundleNameLower];
                    if (hbte != null && (BundleLocation)hbte.Location == BundleLocation.OnLine)
                    {
                        string abPath = files[i];

                        if (File.Exists(abPath))
                        {
                            File.Move(abPath, Path.Combine(tempFolder, Path.GetFileName(abPath)));
                        }

                        string abmanifest = abPath + ".manifest";
                        if (File.Exists(abmanifest))
                        {
                            File.Delete(abmanifest);
                        }
                    }
                }
            }
        }

        static void Recursive(string path, List<string> files, List<string> paths)
        {
            string[] names = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);
            foreach (string filename in names)
            {
                string ext = Path.GetExtension(filename);
                if (ext.Equals(".meta")) continue;
                files.Add(filename.Replace(Path.DirectorySeparatorChar, '/'));
            }

            foreach (string dir in dirs)
            {
                paths.Add(dir.Replace(Path.DirectorySeparatorChar, '/'));
                Recursive(dir,files, paths);
            }
        }


        public static string CalcCSharpMD5(List<string> paths, List<string> files)
        {
            List<string> folders = new List<string>();
            //folders.Add("ThirdParty");
            //folders.Add("TSFramework");
            //folders.Add("Plugins");
            //folders.Add("Gen");
            paths.Clear();
            files.Clear();
            foreach (string f in folders)
            {
                string absPath = Path.Combine(Application.dataPath, f);
                Recursive(absPath, paths, files);
            }

            List<string> csharpFiles = new List<string>();
            foreach (string csf in files)
            {
                if (Path.GetExtension(csf).Equals(".cs"))
                    csharpFiles.Add(csf);
            }

            csharpFiles.Sort();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < csharpFiles.Count; i++)
            {
                string cs = csharpFiles[i];
                string content = File.ReadAllText(cs);
                sb.Append(content);
            }

            return Util.md5(sb.ToString());
        }
    }
}