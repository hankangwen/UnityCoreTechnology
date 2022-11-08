using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapter0 : MonoBehaviour
{
    public GameObject go;

    // private 
    // private Camera mainCamera
    // {
    //     get
    //     {
    //         if()
    //     }
    // }
    
    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Test();
        }
    }
    
    

    void Test()
    {
        Debug.Log(go.transform.position);
        var mainCamera = App.Instance.mainCamera;
        var pos = mainCamera.WorldToScreenPoint(go.transform.position);
        Debug.Log(pos);
        pos = mainCamera.ScreenToWorldPoint(pos);
        Debug.Log(pos);
        
        Debug.Log(Screen.width);
        Debug.Log(Screen.height);
    }
}
