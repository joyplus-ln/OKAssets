# OKAssets
### OKAssets 是一整套Unity3D资源管理系统。包含了AssetBundle、Atlas、Sprite（动态图集）系统。

## 功能特色
- **高灵活度的打包方案**
- **自定义tag**
- **内置的引用计数**
- **多种模式自由切换**

## 快速开始

#### AssetBundle使用

- **初始化**
- **OKAssets.GetInstance().Init()**
- **初始化后可以直接加载本地的所有资源

#### 更新上CDN资源

- **var list = OKAssets.getInstance().CheckUpdate(URL,callBack)**
- **返回线上的basic资源的差异文件以及大小**
- **OKAssets.GetInstance().DownLoadDiffBundles(list)**
- **下载差异文件，下载完成后basic基础资源便跟线上一致了**
-**OKAssets.GetInstance().DownLoadBundleByTag(tag)**
- **下载某个标签的相关资源**

