using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OKAssets
{
    public class BundleInfo
    {
        public string name;
        public long byteSize;
        public string crcOrMD5Hash; //bundle文件的时候，这里存crc的值，非bundle的时候存md5。这个在打包的时候有判定
        public BundleStorageLocation location = BundleStorageLocation.NONE;
        public string bundleTag = "";
        public string nameWithHash = "";
        public List<string> bundles = new List<string>();
        public BundleLocation loactionType = BundleLocation.Local;

        public void Parse(string s)
        {
            string[] fs = s.Split('|');
            if (fs.Length < 7)
            {
                return;
            }

            name = fs[0];
            nameWithHash = fs[1];
            long _byteSize = 0;
            if (long.TryParse(fs[2], out _byteSize))
            {
                byteSize = _byteSize;
            }

            crcOrMD5Hash = fs[3];

            int _location = 0;
            if (int.TryParse(fs[4], out _location))
            {
                location = (BundleStorageLocation)_location;
            }

            bundleTag = fs[5];

            int _locationType = 0;
            if (int.TryParse(fs[6], out _locationType))
            {
                loactionType = (BundleLocation)_locationType;
            }

            List<string> bundleList = new List<string>();
            if (fs.Length >= 8 && !string.IsNullOrEmpty(fs[7]))
            {
                byte[] bytes = Convert.FromBase64String(fs[7]);
                string[] list = Encoding.Default.GetString(bytes).Split('|');
                for (int i = 0; i < list.Length; i++)
                {
                    if (!string.IsNullOrEmpty(list[i]))
                    {
                        bundleList.Add(list[i]);
                    }
                }

                bundles = bundleList;
            }

        }

        public void Update(BundleInfo newInfo)
        {
            name = newInfo.name;
            nameWithHash = newInfo.nameWithHash;
            byteSize = newInfo.byteSize;
            crcOrMD5Hash = newInfo.crcOrMD5Hash;
            location = newInfo.location;
            bundleTag = newInfo.bundleTag;
            loactionType = newInfo.loactionType;
        }

        public string Output()
        {
            string bundleStr = "";
            for (int i = 0; i < bundles.Count; i++)
            {
                bundleStr = $"{bundleStr}|{bundles[i].Replace(OKAssetsConst.ASSET_PATH_PREFIX,"")}";
            }

            byte[] bytes = Encoding.Default.GetBytes(bundleStr);
            string mappingStr = Convert.ToBase64String(bytes);
            return name + "|" + nameWithHash + "|" + byteSize + "|" + crcOrMD5Hash + "|" + (int)location + "|" +
                   bundleTag + "|" +
                   (int)loactionType + "|" + mappingStr;
        }
    }
}