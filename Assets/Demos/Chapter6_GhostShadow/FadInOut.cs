using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadInOut : MonoBehaviour
{
    public float lifeCycle = 2.0f;

    private float _startTime;
    private Material _material;
    private readonly string ColorStr = "_Color";
    private readonly string OutLineColorStr = "_OutlineColor";

    private void Start()
    {
        _startTime = Time.time;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (!meshRenderer || !meshRenderer.material)
        {
            enabled = false;
        }
        else
        {
            _material = meshRenderer.material;
            ReplaceShader();
        }
    }
    
    private void Update()
    {
        float time = Time.time - _startTime;
        if (time > lifeCycle)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            float remainderTime = lifeCycle - time;
            if (_material)
            {
                Color col = _material.GetColor(ColorStr);
                col.a = remainderTime;
                _material.SetColor(ColorStr, col);

                col = _material.GetColor(OutLineColorStr);
                col.a = remainderTime;
                _material.SetColor(OutLineColorStr, col);
            }
        }
    }

    private void ReplaceShader()
    {
        if (_material.shader.name.Equals("Custom/Toon/Basic Outline"))
        {
            _material.shader = Shader.Find("Custom/Toon/Basic Outline Replace");
        }
        else if (_material.shader.name.Equals("Custom/Toon/Basic"))
        {
            _material.shader = Shader.Find("Custom/Toon/Basic Replace");
        }
        else
        {
            Debug.LogError("Can't find target shader");
        }
    }
}
