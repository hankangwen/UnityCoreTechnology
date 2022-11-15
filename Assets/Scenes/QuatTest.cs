using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuatTest : MonoBehaviour
{
    public Vector3 euler = Vector3.zero;
    
    void Update()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        
        //将横向输入转化为左右旋转，将纵向输入转化为俯仰旋转，得到一个很小的旋转四元数
        Quaternion smallRotate = Quaternion.Euler(30, 0, 0);
        
        //将这个小的旋转叠加到当前旋转位置上
        if (Input.GetMouseButtonDown(0))
        {
            // 按住左键时，沿世界坐标轴旋转
            // transform.rotation = smallRotate * transform.rotation;

            Debug.Log(Quaternion.Euler(euler));
            Debug.Log(EulerToQuaternion(euler * Mathf.Deg2Rad));

        }
        else
        {
            // 不按左键时，沿局部坐标轴旋转
            // transform.rotation = transform.rotation * smallRotate;
            transform.rotation = smallRotate;
        }
    }

    Quaternion EulerToQuaternion(Vector3 eulerAngles)
    {
        float cX = Mathf.Cos(eulerAngles.x / 2.0f);
        float sX = Mathf.Sin(eulerAngles.x / 2.0f);

        float cY = Mathf.Cos(eulerAngles.y / 2.0f);
        float sY = Mathf.Sin(eulerAngles.y / 2.0f);
        
        float cZ = Mathf.Cos(eulerAngles.z / 2.0f);
        float sZ = Mathf.Sin(eulerAngles.z / 2.0f);

        Quaternion qX = new Quaternion(sX, 0, 0, cX);
        Quaternion qY = new Quaternion(0, sY, 0, cY);
        Quaternion qZ = new Quaternion(0, 0, sZ, cZ);

        Quaternion ret =  qY *qZ * qX;
        
        return ret;
    }

    float Dot(Quaternion q1, Quaternion q2)
    {
        return (q1.x * q2.x + q1.y * q2.y + q1.z * q2.z + q1.w * q2.w);
    }
}
