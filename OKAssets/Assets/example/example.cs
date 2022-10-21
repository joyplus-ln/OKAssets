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
            OKAsset.GetInstance().CheckUpdate("http://49.234.15.140:8688/",
                (list, size) =>
                {
                    Debug.LogError("inited");
                    GameObject.Instantiate(OKAsset.GetInstance().LoadPrefab("PrefabB/Cube.prefab"));
                }, () => {  Debug.LogError("inited error");
                    GameObject.Instantiate(OKAsset.GetInstance().LoadPrefab("PrefabB/Cube.prefab")); });
        });

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
