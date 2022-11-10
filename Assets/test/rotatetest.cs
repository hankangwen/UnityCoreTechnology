using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class rotatetest : MonoBehaviour
{
    public Transform t1;
    public Transform t2;
    public Transform t3;
    public Transform t4;


    public Transform t5;
    public Transform t6;
    public Transform t7;
    public Transform t8;

    private Matrix4x4 M0;
    private Matrix4x4 H0;


    // Start is called before the first frame update
    void Start()
    {
        M0 = GetMatrix(t1.position,t2.position,t3.position,t4.position);
        H0 = GetMatrix(t5.position,t6.position,t7.position,t8.position);
    }


   
    /*
    [ContextMenu("旋转四元数")]
    private void RotateMatrix()
    {
        this.transform.rotation = getRotation(M0);
    }
    */

    [ContextMenu("旋转Euler")]
    private void RotateEuler()
    {
        this.transform.eulerAngles = GetEuler(GetRotateMatrix());
    }

    private Matrix4x4 GetMatrix(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        p1 = (p1 - p0).normalized;
        p2 = (p2 - p0).normalized;
        p3 = (p3 - p0).normalized;

        Matrix4x4 m4x4 = new Matrix4x4(
        new Vector4(p1.x,p1.y,p1.z,0),
        new Vector4(p2.x, p2.y, p2.z, 0),
        new Vector4(p3.x, p3.y, p3.z, 0),
        new Vector4(0, 0, 0, 1)
        );
        Debug.Log(m4x4);
        return m4x4 ;
    }

    //获取旋转矩阵
    private Matrix4x4 GetRotateMatrix()
    {
      
        Matrix4x4 m4=   H0*M0.inverse;
        Debug.Log(m4);
        return m4;

    }

    /*
    private Quaternion getRotation(Matrix4x4 matrix)
    {

        float q0 = 0.5f*Mathf.Sqrt(1+matrix[0,0]+ matrix[1, 1]+ matrix[2, 2]);
       
        float q1 = 0.25f * (matrix[2, 1] - matrix[1, 2]) / q0;
        float q2 = 0.25f * (matrix[0, 2] - matrix[2, 0]) / q0;
        float q3 = 0.25f * (matrix[1, 0] - matrix[0, 1]) / q0;
        Debug.Log(q0);
        Debug.Log(q1);
        Debug.Log(q2);
        Debug.Log(q3);
        Quaternion quaternion = new Quaternion(q0, q1, q2, q3);
        Debug.Log(quaternion);
        return quaternion;
    }
    */

    //旋转矩阵--》欧拉角   左手坐标系，欧拉角旋转顺序（世界）：Z-X-Y   (Local顺序为Y-X-Z)
    private Vector3 GetEuler(Matrix4x4 matrix)
    {
        float x = Mathf.Atan2(-matrix[1, 2], Mathf.Sqrt(matrix[1, 0] *matrix[1, 0] + matrix[1, 1] * matrix[1, 1]));
        float y = Mathf.Atan2(matrix[0,2], matrix[2,2]);
        float z = Mathf.Atan2(matrix[1,0],matrix[1,1]);
        x = x * 180 / Mathf.PI;
        y = y * 180 / Mathf.PI;
        z = z * 180 / Mathf.PI;
        Vector3 euler = new Vector3(x,y,z);
        Debug.Log(euler);
        return euler;
    }
}
