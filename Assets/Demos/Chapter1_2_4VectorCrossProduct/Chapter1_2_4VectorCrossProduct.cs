using System;
using UnityEngine;

public class Chapter1_2_4VectorCrossProduct : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;

    void Start()
    {
        // Vector3 source = cube.transform.position;
        // Vector3 offset = new Vector3(3, 3, 3);
        // Translate(source, offset);
        // offset = new Vector3(0.5f, 0.5f, 0.5f);
        // Scale(cube.transform.localScale, offset);
        // RotateX(cube.transform.eulerAngles, 30.0f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Quaternion quaternion = cube.transform.rotation;
            Debug.Log(quaternion);
            Debug.Log(quaternion.eulerAngles);
        }
    }

    /* X轴旋转矩阵         
                    |1, 0, 0, 0|    
     [x, y, z, 1] * |0, cosB, sinB, 0| =  [x1, y1, z1, 1]
                    |0, -sinB, cosB, 0|    
                    |0, 0, 0, 1|    
 */
    void RotateX(Vector3 source, float angle)
    {
        float x = source.x + source.y;
        float y = (source.y * (float) Math.Cos(angle)) - (source.z * (float) Math.Sin(angle));
        float z = (source.y * (float) Math.Sin(angle)) + (source.z * (float) Math.Cos(angle));
        cube.transform.eulerAngles = new Vector3(x, y, z);
    }

 /* 平移矩阵         
                    |1, 0, 0, 0|    
     [x, y, z, 1] * |0, 1, 0, 0| =  [x1, y1, z1, 1]
                    |0, 0, 1, 0|    
                    |a, b, c, 1|    
 */
    void Translate(Vector3 source, Vector3 offset)
    {
        float x = source.x * 1 + offset.x * 1;
        float y = source.y * 1 + offset.y * 1;
        float z = source.z * 1 + offset.z * 1;
        cube.transform.position = new Vector3(x, y, z);
    }
    
/* 缩放矩阵         
                    |a, 0, 0, 0|    
     [x, y, z, 1] * |0, b, 0, 0| =  [x1, y1, z1, 1]
                    |0, 0, c, 0|    
                    |0, 0, 0, 1|    
 */
    void Scale(Vector3 source, Vector3 offset)
    {
        float x = source.x * offset.x;
        float y = source.y * offset.y;
        float z = source.z * offset.z;
        cube.transform.localScale = new Vector3(x, y, z);
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
