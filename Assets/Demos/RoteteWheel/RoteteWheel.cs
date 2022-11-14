using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoteteWheel : MonoBehaviour
{
    public GameObject wheelObj;
    private Vector3 _wheelPos;
    private Vector3 _oldPos = Vector3.zero;

    void Start()
    {
        _wheelPos = wheelObj.transform.position;
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.A))
        if(Input.GetMouseButton(0))
        {
            RotateWheel(Input.mousePosition);
        }
    }

    void RotateWheel(Vector3 pos)
    {
        Vector3 curVec = pos - _wheelPos;   //计算方向盘中心点到触控点的向量

        Vector3 normalVec = Vector3.Cross(curVec, _oldPos); //计算法向量
        float vecAngle = Vector2.Angle(curVec, _oldPos);    //计算两个向量的夹角
        
        //使用“右手定则”可知，当大拇指方向指向我们时，四指方向为逆时针方向
        //当大拇指方向远离我们时候，四指方向为顺时针方向
        //这里叉乘后的法向量平行于z轴，所以用法向量的z分量的正负判断法向量方向
        if (normalVec.z > 0)    //和z轴同向，则顺时针转
        {
            wheelObj.transform.Rotate(Vector3.forward, -vecAngle);  //顺时针转动
        }
        else if(normalVec.z < 0)    //和z轴反向，则逆时针转
        {
            wheelObj.transform.Rotate(Vector3.forward, vecAngle);   //逆时针转动
        }

        _oldPos = curVec;
    }
}
