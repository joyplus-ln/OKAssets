
namespace OKAssets
{
    public class LoadedAssetBundle
    {
        public UnityEngine.AssetBundle assetBundle;
        public int referencedCount;

        public LoadedAssetBundle(UnityEngine.AssetBundle assetBundle, int referencedCount)
        {
            this.assetBundle = assetBundle;
            this.referencedCount = referencedCount;
        }

        public int AddReference()
        {
            return ++referencedCount;
        }

        public int DeleteReference()
        {
            return --referencedCount;
        }

        public void Unload()
        {
            referencedCount = 0;
            if (assetBundle == null)
            {
                return;
            }

            assetBundle.Unload(false);
            assetBundle = null;
        }
    }
}