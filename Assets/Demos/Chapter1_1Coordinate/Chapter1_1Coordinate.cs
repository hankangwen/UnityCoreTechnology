using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapter1_1Coordinate : MonoBehaviour
{
    public GameObject go;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Test();
        }
    }
    
    void Test()
    {
        var pos = go.transform.position;
        Debug.Log(pos);
        
        var mainCamera = App.Instance.mainCamera;
        var screenPoint = mainCamera.WorldToScreenPoint(pos);
        Debug.Log(screenPoint);     // (1920.00, 1080.00, 100.00)

        var worldPoint = mainCamera.ScreenToWorldPoint(screenPoint);
        Debug.Log(worldPoint);      // (102.64, 58.74, 90.00)

        var viewportPoint = mainCamera.WorldToViewportPoint(worldPoint);
        Debug.Log(viewportPoint);   // (1.00, 1.00, 100.00)

        viewportPoint = mainCamera.ScreenToViewportPoint(screenPoint);
        Debug.Log(viewportPoint);

        worldPoint = mainCamera.ViewportToWorldPoint(viewportPoint);
        Debug.Log(worldPoint);

        screenPoint = mainCamera.ViewportToScreenPoint(viewportPoint);
        Debug.Log(screenPoint);
    }
}
