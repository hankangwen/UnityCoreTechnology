using System;
using UnityEngine;

public class Chapter1_2_4VectorCrossProduct : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;

    void Start()
    {
        Vector3 v1 = cube.transform.position;
        Vector3 v2 = sphere.transform.position;
        var dir = Vector3Distance(v1, v2);
        Debug.Log(dir);
        dir = Vector3.Distance(v1, v2);
        Debug.Log(dir);
    }

    // 2.向量的减法，求平方用来计算距离
    float Vector3Distance(Vector3 a, Vector3 b)
    {
        float num1 = a.x - b.x;
        float num2 = a.y - b.y;
        float num3 = a.z - b.z;
        return (float) Math.Sqrt((double) num1 * (double) num1 + (double) num2 * (double) num2 +
                                 (double) num3 * (double) num3);
    }
}
