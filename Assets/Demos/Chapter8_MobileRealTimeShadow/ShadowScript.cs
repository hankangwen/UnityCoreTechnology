using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowScript : MonoBehaviour
{
    public Transform obj;

    private MeshRenderer _plane;
    private RenderTexture _rTexture;
    
    void Start()
    {
        _plane = transform.Find("GameObject/Plane").GetComponent<MeshRenderer>();
        Camera shadowCamera = transform.Find("GameObject/Camera").GetComponent<Camera>();
        
        if (!obj) obj = transform;

        _rTexture = new RenderTexture(256, 256, 0);
        _rTexture.name = Random.Range(0, 100).ToString();
        shadowCamera.targetTexture = _rTexture;
    }
    
    void Update()
    {
        _plane.GetComponent<Renderer>().material.mainTexture = _rTexture;
    }
}
