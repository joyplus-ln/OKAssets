using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor.Build.Reporting;
using System;
using System.Linq;
using System.Text;
using UnityEditor.TestTools;

public class BuildScript : Editor
{
    public const string SCRIPTING_DEFINE_SYMBOLS_RELEASE_WINDOWS = "USE_RELEASE_WINDOWS";
    public const string SCRIPTING_DEFINE_SYMBOLS_RELEASE_ANDROID = "USE_RELEASE_ANDROID";
    public const string SCRIPTING_DEFINE_SYMBOLS_RELEASE_IOS = "USE_RELEASE_IOS";


    public delegate void OnIterAssetFile(FileInfo info, AssetImporter importer, string defAbName);

    public static List<string> GetFilesPathList(string folder, string ext, string[] _igroneFilesExt)
    {
        List<string> result = new List<string>();
        string[] igroneFilesExt = _igroneFilesExt;
        string PREFAB_FOLDER_NAME = folder;
        string fullPath = Path.Combine(Application.dataPath, PREFAB_FOLDER_NAME);
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        string[] fullPathSplits = fullPath.Split(Path.DirectorySeparatorChar);
        if (!Directory.Exists(fullPath))
        {
            return null;
        }

        DirectoryInfo dir = new DirectoryInfo(fullPath);
        FileInfo[] files = dir.GetFiles("*" + ext, SearchOption.AllDirectories);
        foreach (FileInfo item in files)
        {
            string itemFullName = item.FullName;
            //如果有忽略的文件，跳过
            bool hasIgrone = false;
            for (int i = 0; i < igroneFilesExt.Length; i++)
            {
                if (itemFullName.EndsWith(igroneFilesExt[i]))
                {
                    hasIgrone = true;
                }
            }

            if (hasIgrone)
            {
                continue;
            }

            string[] splitFullName = itemFullName.Split(Path.DirectorySeparatorChar);
            ArrayList list = new ArrayList();
            for (int i = fullPathSplits.Length; i < splitFullName.Length - 1; i++)
            {
                list.Add(splitFullName[i]);
            }

            string importerPath = "Assets/" + PREFAB_FOLDER_NAME + "/" +
                                  string.Join("/", (string[])list.ToArray(typeof(string))) + "/" + item.Name;
            importerPath = importerPath.Replace("//", "/");
            result.Add(importerPath);
        }

        return result;
    }

    public static void IterAssets(string folder, string[] _igroneFilesExt, OnIterAssetFile onIterAssetFile)
    {
        string[] igroneFilesExt = _igroneFilesExt;
        string PREFAB_FOLDER_NAME = folder;
        string fullPath = Path.Combine(Application.dataPath, PREFAB_FOLDER_NAME);
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        string[] fullPathSplits = fullPath.Split(Path.DirectorySeparatorChar);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        DirectoryInfo dir = new DirectoryInfo(fullPath);
        FileInfo[] files = dir.GetFiles("*", SearchOption.AllDirectories);
        int currIndex = 0;
        foreach (FileInfo item in files)
        {
            currIndex++;
            EditorUtility.DisplayProgressBar("Progress", "请等待", (float)(currIndex / files.Length));
            string itemFullName = item.FullName;
            //如果有忽略的文件，跳过
            bool hasIgrone = false;
            for (int i = 0; i < igroneFilesExt.Length; i++)
            {
                if (itemFullName.EndsWith(igroneFilesExt[i]))
                {
                    hasIgrone = true;
                }
            }

            if (hasIgrone)
            {
                continue;
            }

            string[] splitFullName = itemFullName.Split(Path.DirectorySeparatorChar);
            ArrayList list = new ArrayList();
            for (int i = fullPathSplits.Length; i < splitFullName.Length - 1; i++)
            {
                list.Add(splitFullName[i]);
            }

            string abName = string.Join("_", (string[])list.ToArray(typeof(string))) + '_' +
                            Path.GetFileNameWithoutExtension(item.Name);
            string importerPath = "Assets/" + PREFAB_FOLDER_NAME + "/" +
                                  string.Join("/", (string[])list.ToArray(typeof(string))) + "/" + item.Name;
            importerPath = importerPath.Replace("//", "/");
            AssetImporter importer = AssetImporter.GetAtPath(importerPath);
            if (importer)
            {
                abName = abName.ToLower();
                if (abName.IndexOf(' ') >= 0)
                {
                    Debug.LogError("发现有AssetBundleName中有非法字符:" + importerPath);
                }

                if (onIterAssetFile != null)
                {
                    onIterAssetFile(item, importer, abName);
                }
            }
        }

        EditorUtility.ClearProgressBar();
    }

    public static void SetIconTextureSettings()
    {
        IterAssets("Res/Icon", new string[] { ".meta" },
            delegate(FileInfo item, AssetImporter importer, string defAbName)
            {
                string dir = Path.GetDirectoryName(item.FullName) + Path.DirectorySeparatorChar +
                             Path.GetFileNameWithoutExtension(item.FullName);
                string lowerDir = dir.ToLower();
                string _lowerDir = lowerDir.Replace(Path.DirectorySeparatorChar, '_');
                string abName = _lowerDir.Substring(_lowerDir.IndexOf("res_") + "res_".Length);

                if (_lowerDir.IndexOf("icon") >= 0)
                {
                    TextureImporter texImporter = importer as TextureImporter;
                    //不处理类型为“Lightmap”的Texture
                    if ("Lightmap" != texImporter.textureType.ToString())
                    {
                        //修改 PackingTag
                        texImporter.spritePackingTag = "";

                        SetPlatformIconTextureSettings(texImporter);

                        AssetDatabase.ImportAsset(texImporter.assetPath);
                    }
                }
            });
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
    }

    public static void SetUITextureSettings()
    {
        IterAssets("Res/UI", new string[] { ".meta" }, delegate(FileInfo item, AssetImporter importer, string defAbName)
        {
            string dir = Path.GetDirectoryName(item.FullName);
            string lowerDir = dir.ToLower();
            string _lowerDir = lowerDir.Replace(Path.DirectorySeparatorChar, '_').Replace("/", "_");
            string abName = "";
            if (_lowerDir.IndexOf("ui_texture") >= 0)
            {
                abName = _lowerDir.Substring(_lowerDir.IndexOf("ui_texture"));
                TextureImporter texImporter = importer as TextureImporter;
                //不处理类型为“Lightmap”的Texture
                if ("Lightmap" != texImporter.textureType.ToString())
                {
                    //修改 PackingTag
                    texImporter.spritePackingTag = abName;

                    SetPlatformUITextureSettings(texImporter);

                    AssetDatabase.ImportAsset(texImporter.assetPath);
                }
            }
        });
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
    }

    public static void SetPlatformIconTextureSettings(TextureImporter texImporter)
    {
        //修改Texture Type
        texImporter.textureType = TextureImporterType.Sprite;
        //修改Aniso Level
        texImporter.anisoLevel = 9;
        //修改Read/Write enabled 
        //texImporter.isReadable = false;
        //修改Generate Mip Maps
        texImporter.mipmapEnabled = false;

        TextureImporterPlatformSettings tt_def = texImporter.GetDefaultPlatformTextureSettings();
        tt_def.textureCompression = TextureImporterCompression.CompressedLQ;
        tt_def.maxTextureSize = 2048;
        tt_def.crunchedCompression = false;
        texImporter.SetPlatformTextureSettings(tt_def);

        TextureImporterPlatformSettings tt_standalone = new TextureImporterPlatformSettings
        {
            name = "Standalone",
            maxTextureSize = 2048,
            format = texImporter.DoesSourceTextureHaveAlpha()
                ? TextureImporterFormat.RGBA32
                : TextureImporterFormat.RGB24,
            overridden = true,
        };
        texImporter.SetPlatformTextureSettings(tt_standalone);

        TextureImporterPlatformSettings tt_android = new TextureImporterPlatformSettings
        {
            name = "Android",
            maxTextureSize = 2048,
            format = TextureImporterFormat.ASTC_6x6,
            textureCompression = TextureImporterCompression.CompressedLQ,
            crunchedCompression = false,
            overridden = true,
            androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings,
        };
        texImporter.SetPlatformTextureSettings(tt_android);

        TextureImporterPlatformSettings tt_ios = new TextureImporterPlatformSettings
        {
            name = "iPhone",
            maxTextureSize = 2048,
            format = TextureImporterFormat.ASTC_6x6,
            textureCompression = TextureImporterCompression.CompressedHQ,
            crunchedCompression = false,
            overridden = true,
        };
        texImporter.SetPlatformTextureSettings(tt_ios);

        TextureImporterSettings tis = new TextureImporterSettings();
        texImporter.ReadTextureSettings(tis);
        tis.spriteMeshType = SpriteMeshType.FullRect;
        texImporter.SetTextureSettings(tis);
    }

    public static void SetPlatformUITextureSettings(TextureImporter texImporter)
    {
        SetPlatformIconTextureSettings(texImporter);
        TextureImporterSettings tis = new TextureImporterSettings();
        texImporter.ReadTextureSettings(tis);
        tis.spriteMeshType = SpriteMeshType.Tight;
        texImporter.SetTextureSettings(tis);
    }

    public static void SetPlatformTextureSettings(TextureImporter texImporter, int limitMaxTextureSize = 1024)
    {
        if (texImporter.textureType != TextureImporterType.Default &&
            texImporter.textureType != TextureImporterType.NormalMap)
        {
            return;
        }

        int targetMaxTextureSize = 0;
        int originalMaxTextureSize = 0;
        TextureImporterFormat originalPlatformTextureFmt;
        // if (texImporter.GetPlatformTextureSettings("Standalone", out originalMaxTextureSize, out originalPlatformTextureFmt))
        // {
        // 	targetMaxTextureSize = originalMaxTextureSize;
        // }
        // else
        // {
        targetMaxTextureSize = texImporter.GetDefaultPlatformTextureSettings().maxTextureSize;
        // }
        targetMaxTextureSize = Mathf.Clamp(targetMaxTextureSize, 0, limitMaxTextureSize);


        TextureImporterPlatformSettings tt_def = texImporter.GetDefaultPlatformTextureSettings();
        tt_def.maxTextureSize = targetMaxTextureSize;
        texImporter.SetPlatformTextureSettings(tt_def);

        TextureImporterPlatformSettings tt_standalone = new TextureImporterPlatformSettings
        {
            name = "Standalone",
            maxTextureSize = targetMaxTextureSize,
            format = (texImporter.DoesSourceTextureHaveAlpha() ||
                      texImporter.textureType == TextureImporterType.NormalMap)
                ? TextureImporterFormat.RGBA32
                : TextureImporterFormat.RGB24,
            overridden = true,
        };
        texImporter.SetPlatformTextureSettings(tt_standalone);

        TextureImporterPlatformSettings tt_android = new TextureImporterPlatformSettings
        {
            name = "Android",
            maxTextureSize = targetMaxTextureSize,
            format = TextureImporterFormat.ASTC_6x6,
            textureCompression = TextureImporterCompression.CompressedLQ,
            crunchedCompression = false,
            overridden = true,
            androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings,
        };
        texImporter.SetPlatformTextureSettings(tt_android);

        TextureImporterPlatformSettings tt_ios = new TextureImporterPlatformSettings
        {
            name = "iPhone",
            maxTextureSize = targetMaxTextureSize,
            format = TextureImporterFormat.ASTC_6x6,
            textureCompression = TextureImporterCompression.CompressedHQ,
            crunchedCompression = false,
            overridden = true,
        };
        texImporter.SetPlatformTextureSettings(tt_ios);
    }


    static void GetAllDirs(string dir, List<string> list)
    {
        string[] dirs = Directory.GetDirectories(dir);
        list.AddRange(dirs);

        for (int i = 0; i < dirs.Length; i++)
        {
            GetAllDirs(dirs[i], list);
        }
    }

    
    public static BuildPlayerOptions GetBuildPlayerOptions()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        List<string> sceneStrList = new List<string>();
        //foreach (EditorBuildSettingsScene s in scenes)
        //{
        //	sceneStrList.Add(s.path);
        //	Debug.Log("Add [" + s.path + "] to build scene list");
        //}
        sceneStrList.Add(scenes[0].path);
        Debug.Log("Add [" + scenes[0].path + "] to build scene list");
        buildPlayerOptions.scenes = sceneStrList.ToArray();
        return buildPlayerOptions;
    }
}