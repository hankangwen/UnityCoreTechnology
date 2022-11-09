using System;
using UnityEngine;

public class Chapter1_2_3VectorDotProduct : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;

    //3.点乘求角度和投影
    void Start()
    {
        Vector3 a = new Vector3(2,1,0);
        Vector3 b = new Vector3(3,0,0);
        var dir_b = Vector3.Normalize(b);      //dir_b是标准化的向量b
        float pa = Vector3.Dot(a,dir_b);        //pa即是向量a在向量b方向的投影长度
        Debug.Log(pa);
    }
}
