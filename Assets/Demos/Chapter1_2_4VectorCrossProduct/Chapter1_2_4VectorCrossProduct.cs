using System;
using UnityEngine;

public class Chapter1_2_4VectorCrossProduct : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;
/*
 * 1    1   1
 * 4    5   6
 * 3    4   5
 */
    void Start()
    {
        Vector3 source = cube.transform.position;
        Vector3 offset = new Vector3(3, 4, 5);
        Translate(source, offset);
    }

 /* 平移矩阵         
                    |1, 0, 0, 0|    
     [x, y, z, 1] * |0, 1, 0, 0| =  [x1, y1, z1, 1]
                    |0, 0, 1, 0|    
                    |a, b, c, 1|    
 */
    void Translate(Vector3 source, Vector3 offset)
    {
        float x = source.x * (1 + offset.x);
        float y = source.y * (1 + offset.y);
        float z = source.z * (1 + offset.z);
        cube.transform.position = new Vector3(x, y, z);
    }

    //4.叉乘求法线
    void CrossProduct()
    {
        Vector3 a = new Vector3(2,1,1);    //a和b是某个平面上的任意两个向量
        Vector3 b = new Vector3(3,0,2);
        Vector3 n = Vector3.Cross(a,b);   //n是该平面的法线
        n = Vector3.Normalize(n);                //将n标准化
        Debug.Log(n);
    }
}
