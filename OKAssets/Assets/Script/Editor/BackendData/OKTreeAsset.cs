using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;

namespace OKAssets.Editor
{

	[CreateAssetMenu(fileName = "OKAssetBundlesData", menuName = "OKAssetBundlesDataAsset", order = 1)]
	public class OKTreeAsset : ScriptableObject
	{
		[SerializeField]
		public List<OKBundlesTreeElement> treeElements;
		public void UpdateData()
		{
			if (treeElements == null)
			{
				treeElements = new List<OKBundlesTreeElement>();
			}
			ClearOldFolderIsNewFlag();
			List<OKBundlesTreeElement> list = new List<OKBundlesTreeElement>();
			string rootPath = Application.dataPath + $"/{OKAssetsConst.BUNDLEFOLDER}";
			if (!Directory.Exists(rootPath))
			{
				Directory.CreateDirectory(rootPath);
			}
			Debug.Log(rootPath);
			int depth = -1;
			var root = new OKBundlesTreeElement("Root", depth, GenerateIntID(), rootPath);
			Debug.LogError(root.path);
			list.Add(root);
			IterFiles(list, rootPath, depth + 1);
			treeElements = list;
		}

		private void IterFiles(List<OKBundlesTreeElement> list, string rootPath, int depth)
		{
			DirectoryInfo folder = new DirectoryInfo(rootPath);
			DirectoryInfo[] dirInfo = folder.GetDirectories();
			foreach (DirectoryInfo nextFolder in dirInfo)
			{
				OKBundlesTreeElement fItem = new OKBundlesTreeElement(nextFolder.Name, depth, GenerateIntID(), FullPathToBundlePath(nextFolder.FullName));
				fItem.isFolder = true;
				if (HasFolderInfo(fItem.path))
				{
					OKBundlesTreeElement old = GetFolerInfo(fItem.path);
					fItem.CopyFrom(old);
				}
				else
				{

					fItem.isNew = true;
				}
				list.Add(fItem);
				IterFiles(list, nextFolder.FullName, depth + 1);
			}
		}

		private bool HasFolderInfo(string path)
		{
			foreach (OKBundlesTreeElement old in treeElements)
			{
				if (old.path.Equals(path))
				{
					return true;
				}
			}
			return false;
		}

		private OKBundlesTreeElement GetFolerInfo(string path)
		{
			foreach (OKBundlesTreeElement old in treeElements)
			{
				if (old.path.Equals(path))
				{
					return old;
				}
			}
			return null;
		}

		private void ClearOldFolderIsNewFlag()
		{
			foreach (OKBundlesTreeElement old in treeElements)
			{
				old.isNew = false;
			}
		}

		private int GenerateIntID()
		{
			byte[] buffer = Guid.NewGuid().ToByteArray();
			return BitConverter.ToInt32(buffer, 0);
		}

		//   such as D:\\taro\nclient\Assets\Res\UI -> \UI
		private string FullPathToBundlePath(string fullPath)
		{
			string rootPath = OKAssetsConst.ASSET_PATH_PREFIX;
			string path = fullPath.Replace('\\', '/');
			path = path.Substring(path.IndexOf(rootPath) + rootPath.Length);
			return path;
		}

	}
}
