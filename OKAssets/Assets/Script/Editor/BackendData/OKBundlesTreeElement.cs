using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;
using System;

namespace OKAssets.Editor
{
	[Serializable]
	public class OKBundlesTreeElement : BaseTreeElement
	{
		[SerializeField]
		private string _path;
		public bool isFolder = false;
		public bool isNew = false;
		[SerializeField]
		private int _folderBundleType = (int)HybridBundlesConsts.BundlePackageType.NONE;
		[SerializeField]
		public string bundleName = "";
		[SerializeField] 
		private string _bundleTag = "";
		[SerializeField] 
		private int _location = 0;
		public string bundleTag
		{
			get
			{
				return _bundleTag;
			}

			set
			{
				_bundleTag = value;
			}
		}
		
		public int Location
		{
			get
			{
				return _location;
			}

			set
			{
				_location = value;
			}
		}
		public int folderBundleType
		{
			get
			{
				return _folderBundleType;
			}

			set
			{
				_folderBundleType = value;
				bundleName = HybridBundlesConsts.GetFolderBundleNameForEditor(_path, this);
			}
		}

		public string path
		{
			get
			{
				return _path;
			}

			set
			{
				_path = value;
				bundleName = HybridBundlesConsts.GetFolderBundleNameForEditor(_path, this);
			}
		}

		public OKBundlesTreeElement(string name, int depth, int id, string _path) : base(name, depth, id)
		{
			path = _path;
		}

		public void SetFolderBundleTypeAndDeepChildren(HybridBundlesConsts.BundlePackageType t)
		{
			folderBundleType = (int)t;
			if (hasChildren)
			{
				foreach (OKBundlesTreeElement element in children)
				{
					element.SetFolderBundleTypeAndDeepChildren(t);
				}
			}
		}

		public void CopyFrom(OKBundlesTreeElement source)
		{

			path = source.path;
			folderBundleType = source.folderBundleType;
			bundleTag = source.bundleTag;
			Location = source.Location;
		}

		public void SetFolderBundleTagAndDeepChildren(string tag)
		{
			bundleTag = tag;
			if (hasChildren)
			{
				foreach (OKBundlesTreeElement element in children)
				{
					element.SetFolderBundleTagAndDeepChildren(tag);
				}
			}
		}
		
		public void SetFolderBundleLocationAndDeepChildren(int location)
		{
			Location = location;
			if (hasChildren)
			{
				foreach (OKBundlesTreeElement element in children)
				{
					element.SetFolderBundleLocationAndDeepChildren(location);
				}
			}
		}
	}
}