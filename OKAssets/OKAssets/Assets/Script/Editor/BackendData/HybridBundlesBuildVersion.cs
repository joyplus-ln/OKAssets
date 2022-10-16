using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OKAssets.Editor
{
	[CreateAssetMenu(fileName = "OKAssetBundlesBuildVersionData", menuName = "OKAssetBundlesBuildVersionAsset", order = 1)]
	class HybridBundlesBuildVersion : ScriptableObject
	{
		public string bundleVersion = "0.0.0.0";
		public string csharpMD5 = "adfasfsd";
	}
}
