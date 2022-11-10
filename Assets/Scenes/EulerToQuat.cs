using System;
using System.Reflection;
using UnityEngine;

public class EulerToQuat : MonoBehaviour
{
    public Vector3 euler = Vector3.zero;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"euler = { euler }");
            Debug.Log($"引擎结果 = { Quaternion.Euler(euler) }");
            Debug.Log($"本文结果 = { EulerToQuaternion(euler * Mathf.Deg2Rad) }");
        }
    }

    Quaternion EulerToQuaternion(Vector3 eulerAngles)
    {
        RotationOrder order = GetRotationOrder();
        return EulerToQuaternionInternal(eulerAngles, order);
    }
    
    Quaternion EulerToQuaternionInternal(Vector3 eulerAngles, RotationOrder order)
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
        Quaternion ret = new Quaternion();
        
        switch (order)
        {
            case RotationOrder.OrderZYX: ret = qX * qY * qZ; break;
            case RotationOrder.OrderYZX: ret = qX * qZ * qY; break;
            case RotationOrder.OrderXZY: ret = qY * qZ * qX; break;
            case RotationOrder.OrderZXY: ret = qY * qX * qZ; break;
            case RotationOrder.OrderYXZ: ret = qZ * qX * qY; break;
            case RotationOrder.OrderXYZ: ret = qZ * qY * qX; break;
        }
        
        return ret;
    }

    RotationOrder GetRotationOrder()
    {
        Transform mTransform = this.transform;
        
        Type transformType = mTransform.GetType();
        PropertyInfo m_propertyInfo_rotationOrder = transformType.GetProperty("rotationOrder", 
            BindingFlags.Instance | BindingFlags.NonPublic);
        object m_OldRotationOrder = m_propertyInfo_rotationOrder.GetValue(mTransform, null);
        return (RotationOrder)m_OldRotationOrder;
    }
    
    enum RotationOrder
    {
        OrderXYZ,
        OrderXZY,
        OrderYZX,
        OrderYXZ,
        OrderZXY,
        OrderZYX,
    }
}
