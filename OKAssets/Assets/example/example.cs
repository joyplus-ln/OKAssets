using System.Collections;
using System.Collections.Generic;
using OKAssets;
using UnityEngine;

public class example : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        OKAsset.GetInstance().Init(() =>
        {
            OKAsset.GetInstance().CheckUpdate("http://49.234.15.140:8688/debug/A",
                (list, size) =>
                {
                    if (list.Length > 0)
                    {
                        Debug.LogError("xuyaoxiazai");
                        OKAsset.GetInstance().DownLoadDiffBundles(list, (x) =>
                        {
                            Debug.LogError("ok" + x);
                        }, (list) =>
                        {
                            Debug.LogError("error" + list.Count);
                        });
                    }
                    else
                    {
                        Debug.LogError("inited");
                        GameObject.Instantiate(OKAsset.GetInstance().LoadPrefab("Prefab/Sphere.prefab"));
                    }
                   
                   
                }, () => {  Debug.LogError("inited error");
                    GameObject.Instantiate(OKAsset.GetInstance().LoadPrefab("Prefab/Sphere.prefab")); });
        });

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
