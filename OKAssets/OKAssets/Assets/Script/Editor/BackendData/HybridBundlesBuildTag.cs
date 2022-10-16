using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OKAssets.Editor
{
	[CreateAssetMenu(fileName = "OKAssetBundlesBuildTagData", menuName = "OKAssetBundlesBuildTagAsset", order = 1)]
	public class HybridBundlesBuildTag : ScriptableObject
	{
		public const string Basic = "basic";
		public String[] tags = new []{Basic};
	}
}
