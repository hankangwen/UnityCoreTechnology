using UnityEngine;

public class ShadowScript : MonoBehaviour
{
    public Transform obj;

    private GameObject _plane;
    private RenderTexture _rTexture;
    
    void Start()
    {
        _plane = transform.Find("Shadow/Plane").gameObject;
        Camera shadowCamera = transform.Find("Shadow/Camera").GetComponent<Camera>();
        
        if (!obj) obj = transform;

        _rTexture = new RenderTexture(256, 256, 0);
        _rTexture.name = Random.Range(0, 100).ToString();
        shadowCamera.targetTexture = _rTexture;
        
        _plane.GetComponent<Renderer>().material.mainTexture = _rTexture;
    }
    
    // void Update()
    // {
    //     _plane.GetComponent<Renderer>().material.mainTexture = _rTexture;
    // }
}