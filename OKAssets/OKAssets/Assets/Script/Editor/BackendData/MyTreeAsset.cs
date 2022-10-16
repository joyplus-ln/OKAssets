using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;

namespace OKAssets.Editor
{

	[CreateAssetMenu(fileName = "OKAssetBundlesData", menuName = "OKAssetBundlesDataAsset", order = 1)]
	public class MyTreeAsset : ScriptableObject
	{
		[SerializeField]
		public List<HybridBundlesTreeElement> treeElements;
		public void UpdateData()
		{
			if (treeElements == null)
			{
				treeElements = new List<HybridBundlesTreeElement>();
			}
			ClearOldFolderIsNewFlag();
			List<HybridBundlesTreeElement> list = new List<HybridBundlesTreeElement>();
			string rootPath = Application.dataPath + $"/{OKAssetsConst.okConfig.ResFolderName}";
			if (!Directory.Exists(rootPath))
			{
				Directory.CreateDirectory(rootPath);
			}
			int depth = -1;
			var root = new HybridBundlesTreeElement("Root", depth, GenerateIntID(), rootPath);
			list.Add(root);
			IterFiles(list, rootPath, depth + 1);
			treeElements = list;
		}

		private void IterFiles(List<HybridBundlesTreeElement> list, string rootPath, int depth)
		{
			DirectoryInfo folder = new DirectoryInfo(rootPath);
			DirectoryInfo[] dirInfo = folder.GetDirectories();
			foreach (DirectoryInfo nextFolder in dirInfo)
			{
				HybridBundlesTreeElement fItem = new HybridBundlesTreeElement(nextFolder.Name, depth, GenerateIntID(), FullPathToBundlePath(nextFolder.FullName));
				fItem.isFolder = true;
				if (HasFolderInfo(fItem.path))
				{
					HybridBundlesTreeElement old = GetFolerInfo(fItem.path);
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
			foreach (HybridBundlesTreeElement old in treeElements)
			{
				if (old.path.Equals(path))
				{
					return true;
				}
			}
			return false;
		}

		private HybridBundlesTreeElement GetFolerInfo(string path)
		{
			foreach (HybridBundlesTreeElement old in treeElements)
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
			foreach (HybridBundlesTreeElement old in treeElements)
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
			string rootPath = Application.dataPath + "/Res";
			string path = fullPath.Replace('\\', '/');
			path = path.Substring(path.IndexOf(rootPath) + rootPath.Length);
			return path;
		}

	}
}
