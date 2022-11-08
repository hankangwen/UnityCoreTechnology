using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class App
{
    private static App _instance;

    public static App Instance
    {
        get
        {
            if (_instance == null)
                _instance = new App();
            return _instance;
        }
    }

    private Camera _mainCamera;
    public Camera mainCamera
    {
        get
        {
            if(_mainCamera==null)
                _mainCamera = Camera.main;
            return _mainCamera;
        }
    }
}
