using System;
using UnityEngine;

public class Chapter1_2_1VectorAdd : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;
    public float factor = 0f;
    
    private float _lastFactor;
    private Vector3 _normalVector;
    private Vector3 _initPos;

    void Start()
    {
        _lastFactor = factor;
        _initPos = cube.transform.position;
        VectorAdd();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
            VectorAdd();

        if (Math.Abs(factor - _lastFactor) > 0.0001f)
        {
            _lastFactor = factor;
            var pos = _initPos + factor * _normalVector;
            Debug.Log(pos);
            cube.transform.position = pos;
        }
    }
    
    // 1.向量加法, 物体位移
    void VectorAdd()
    {
        Vector3 v1 = cube.transform.position;
        Vector3 v2 = sphere.transform.position;
        Vector3 v3 = v2 - v1;
        Vector3 v4 = Vector3.Normalize(v3);
        Debug.Log(v1);
        Debug.Log(v2);
        Debug.Log(v3);
        Debug.Log(v4);
        _normalVector = v4;
            
        Vector3 v5 = v1;
        for (int i = 0; i <= 4; i++)
        {
            v5 = v5 + v4;
            Debug.Log(v5);
        }

    }
    
    
}
