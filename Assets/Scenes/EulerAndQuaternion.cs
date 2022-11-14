using System;
using System.Reflection;
using UnityEngine;

public class EulerAndQuaternion : MonoBehaviour
{
    public Vector3 euler = Vector3.zero;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            euler = transform.rotation.eulerAngles;
            Debug.Log($"euler = { euler }");
            
            // 欧拉角转四元素
            Quaternion quaternion = Quaternion.Euler(euler);
            Debug.Log(quaternion.eulerAngles);
            Debug.Log($"引擎结果 = { quaternion }");
            
            quaternion = EulerToQuaternion(euler * Mathf.Deg2Rad);
            Debug.Log(quaternion.eulerAngles);
            Debug.Log($"本文结果 = { quaternion }");

            // 四元素转欧拉角
            Vector3 outEuler = Internal_MakePositive(quaternion.ToEuler() * Mathf.Rad2Deg);
            Debug.Log(outEuler);
            outEuler = quaternion.eulerAngles;
            Debug.Log(outEuler);

            outEuler = QuaternionToEuler(quaternion);
            Debug.Log(outEuler);
        }
    }

    #region Tools

    // Makes euler angles positive 0/360 with 0.0001 hacked to support old behaviour of QuaternionToEuler
    private Vector3 Internal_MakePositive(Vector3 euler)
    {
        float negativeFlip = -0.0001f * Mathf.Rad2Deg;
        float positiveFlip = 360.0f + negativeFlip;

        if (euler.x < negativeFlip)
            euler.x += 360.0f;
        else if (euler.x > positiveFlip)
            euler.x -= 360.0f;

        if (euler.y < negativeFlip)
            euler.y += 360.0f;
        else if (euler.y > positiveFlip)
            euler.y -= 360.0f;

        if (euler.z < negativeFlip)
            euler.z += 360.0f;
        else if (euler.z > positiveFlip)
            euler.z -= 360.0f;

        return euler;
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

    #endregion
    
    #region EulerToQuaternion
    
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
    
    #endregion
    
    #region QuaternionToEuler
    
    Vector3 QuaternionToEuler(Quaternion quaternion)
    {
        RotationOrder order = GetRotationOrder();
        quaternion = Quaternion.Normalize(quaternion);
        return QuaternionToEulerInternal(quaternion, order) * Mathf.Rad2Deg;
    }

    Vector3 QuaternionToEulerInternal(Quaternion q, RotationOrder order)
    {
        //setup all needed values
        float[] d = new float[(int)QuatIndexes.QuatIndexesCount] {q.x * q.x, q.x * q.y, q.x * q.z, q.x * q.w, q.y * q.y, q.y * q.z, q.y * q.w, q.z * q.z, q.z * q.w, q.w * q.w};
        //Float array for values needed to calculate the angles
        float[] v = new float [(int) Indexes.IndexesCount];// { 0.0f };
        for (int i = 0; i < v.Length; i++) v[i] = 0.0f;
        
        
        
        // qFunc f[3] = {qFuncs[order][0], qFuncs[order][1], qFuncs[order][2]}; //functions to be used to calculate angles
        
        /*
        

    

    const float SINGULARITY_CUTOFF = 0.499999f;
    Vector3f rot;
    switch (order)
    {
        case math::kOrderZYX:
            v[singularity_test] = d[xz] + d[yw];
            v[Z1] = 2.0f * (-d[xy] + d[zw]);
            v[Z2] = d[xx] - d[zz] - d[yy] + d[ww];
            v[Y1] = 1.0f;
            v[Y2] = 2.0f * v[singularity_test];
            if (Abs(v[singularity_test]) < SINGULARITY_CUTOFF)
            {
                v[X1] = 2.0f * (-d[yz] + d[xw]);
                v[X2] = d[zz] - d[yy] - d[xx] + d[ww];
            }
            else //x == xzx z == 0
            {
                float a, b, c, e;
                a = d[xz] + d[yw];
                b = -d[xy] + d[zw];
                c = d[xz] - d[yw];
                e = d[xy] + d[zw];

                v[X1] = a * e + b * c;
                v[X2] = b * e - a * c;
                f[2] = &qNull;
            }
            break;
        case math::kOrderXZY:
            v[singularity_test] = d[xy] + d[zw];
            v[X1] = 2.0f * (-d[yz] + d[xw]);
            v[X2] = d[yy] - d[zz] - d[xx] + d[ww];
            v[Z1] = 1.0f;
            v[Z2] = 2.0f * v[singularity_test];

            if (Abs(v[singularity_test]) < SINGULARITY_CUTOFF)
            {
                v[Y1] = 2.0f * (-d[xz] + d[yw]);
                v[Y2] = d[xx] - d[zz] - d[yy] + d[ww];
            }
            else //y == yxy x == 0
            {
                float a, b, c, e;
                a = d[xy] + d[zw];
                b = -d[yz] + d[xw];
                c = d[xy] - d[zw];
                e = d[yz] + d[xw];

                v[Y1] = a * e + b * c;
                v[Y2] = b * e - a * c;
                f[0] = &qNull;
            }
            break;

        case math::kOrderYZX:
            v[singularity_test] = d[xy] - d[zw];
            v[Y1] = 2.0f * (d[xz] + d[yw]);
            v[Y2] = d[xx] - d[zz] - d[yy] + d[ww];
            v[Z1] = -1.0f;
            v[Z2] = 2.0f * v[singularity_test];

            if (Abs(v[singularity_test]) < SINGULARITY_CUTOFF)
            {
                v[X1] = 2.0f * (d[yz] + d[xw]);
                v[X2] = d[yy] - d[xx] - d[zz] + d[ww];
            }
            else //x == xyx y == 0
            {
                float a, b, c, e;
                a = d[xy] - d[zw];
                b = d[xz] + d[yw];
                c = d[xy] + d[zw];
                e = -d[xz] + d[yw];

                v[X1] = a * e + b * c;
                v[X2] = b * e - a * c;
                f[1] = &qNull;
            }
            break;
        case math::kOrderZXY:
        {
            v[singularity_test] = d[yz] - d[xw];
            v[Z1] = 2.0f * (d[xy] + d[zw]);
            v[Z2] = d[yy] - d[zz] - d[xx] + d[ww];
            v[X1] = -1.0f;
            v[X2] = 2.0f * v[singularity_test];

            if (Abs(v[singularity_test]) < SINGULARITY_CUTOFF)
            {
                v[Y1] = 2.0f * (d[xz] + d[yw]);
                v[Y2] = d[zz] - d[xx] - d[yy] + d[ww];
            }
            else //x == yzy z == 0
            {
                float a, b, c, e;
                a = d[xy] + d[zw];
                b = -d[yz] + d[xw];
                c = d[xy] - d[zw];
                e = d[yz] + d[xw];

                v[Y1] = a * e + b * c;
                v[Y2] = b * e - a * c;
                f[2] = &qNull;
            }
        }
        break;
        case math::kOrderYXZ:
            v[singularity_test] = d[yz] + d[xw];
            v[Y1] = 2.0f * (-d[xz] + d[yw]);
            v[Y2] = d[zz] - d[yy] - d[xx] + d[ww];
            v[X1] = 1.0f;
            v[X2] = 2.0f * v[singularity_test];

            if (Abs(v[singularity_test]) < SINGULARITY_CUTOFF)
            {
                v[Z1] = 2.0f * (-d[xy] + d[zw]);
                v[Z2] = d[yy] - d[zz] - d[xx] + d[ww];
            }
            else //x == zyz y == 0
            {
                float a, b, c, e;
                a = d[yz] + d[xw];
                b = -d[xz] + d[yw];
                c = d[yz] - d[xw];
                e = d[xz] + d[yw];

                v[Z1] = a * e + b * c;
                v[Z2] = b * e - a * c;
                f[1] = &qNull;
            }
            break;
        case math::kOrderXYZ:
            v[singularity_test] = d[xz] - d[yw];
            v[X1] = 2.0f * (d[yz] + d[xw]);
            v[X2] = d[zz] - d[yy] - d[xx] + d[ww];
            v[Y1] = -1.0f;
            v[Y2] = 2.0f * v[singularity_test];

            if (Abs(v[singularity_test]) < SINGULARITY_CUTOFF)
            {
                v[Z1] = 2.0f * (d[xy] + d[zw]);
                v[Z2] = d[xx] - d[zz] - d[yy] + d[ww];
            }
            else //x == zxz x == 0
            {
                float a, b, c, e;
                a = d[xz] - d[yw];
                b = d[yz] + d[xw];
                c = d[xz] + d[yw];
                e = -d[yz] + d[xw];

                v[Z1] = a * e + b * c;
                v[Z2] = b * e - a * c;
                f[0] = &qNull;
            }
            break;
    }

    rot = Vector3f(f[0](v[X1], v[X2]),
        f[1](v[Y1], v[Y2]),
        f[2](v[Z1], v[Z2]));

    Assert(IsFinite(rot));

    return rot;
    */

        Vector3 result = new Vector3();
        return result;
    }
    
    //Indexes for values used to calculate euler angles
    enum Indexes
    {
        X1,
        X2,
        Y1,
        Y2,
        Z1,
        Z2,
        singularity_test,
        IndexesCount
    };

    //indexes for pre-multiplied quaternion values
    enum QuatIndexes
    {
        xx,
        xy,
        xz,
        xw,
        yy,
        yz,
        yw,
        zz,
        zw,
        ww,
        QuatIndexesCount
    };
    
    #endregion
}
