Shader "Game/Reflective/SpecularRim"
{
    Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess ("Shininess", Range(0.01, 1)) = 0.078125
        _ReflectColor ("Reflection Color", Color) = (1, 1, 1, 0.5)
        _MainTex ("Base(RGB) Gloss(A)", 2D) = "white" {}
        _Cube ("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeREflect }
        _BumpMap ("Normalmap", 2D) = "bump" {}
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
        _RimColor ("Rim Color", Color) = (0.26, 0.19, 0.16, 0.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
    }
    SubShader
    {
        LOD 300
        Tags { "RenderType"="Gemoetry" }
        
        CGPROGRAM
        #pragma surface surf Lambert
        #pragma target 3.0
        #pragma exclude_renderers d3d11_9x

        sampler2D _MainTex;
        samplerCUBE _Cube;
        sampler2D _BumpMap;
        fixed4 _Color;
        fixed4 _ReflectColor;
        half _Shininess;
        float _RimPower;
        float4 _RimColor;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldRefl;
            float3 viewDir;
            float2 uv_BumpMap;
            INTERNAL_DATA
        };

        //该函数使用了Unity库提供的结构体SurfaceOutput
        /*
         struct SurfaceOutput {
            fixed3 Albedo;  //漫反射颜色
            fixed3 Normal;  //法线
            fixed3 Emission;//自发光颜色
            half Specular;  //镜面反射系数
            fixed Gloss;    //亮度系数
            fixed Alpha;    //透明值
        };
         */
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 c = tex * _Color;
            o.Albedo = c.rgb;
            o.Gloss = tex.a;
            o.Specular = _Shininess;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
	        float3 worldRefl = WorldReflectionVector (IN, o.Normal);
            fixed4 reflcol = texCUBE(_Cube, worldRefl);//材质立方体反射函数
            reflcol *= tex.a;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));//边缘着色函数
            o.Emission = reflcol.rgb * _ReflectColor.rgb + _RimColor.rgb * pow(rim, _RimPower);//自发光函数
            o.Alpha = reflcol.a * _ReflectColor.a;
            o.Specular *= o.Alpha * _Shininess;
        }
        ENDCG
    }
    FallBack "Reflective/VertexLit"
}
