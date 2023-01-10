Shader "WhalesGame/Reflective/SeparateSpecularRim" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_Cube ("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }
	_GrayTex("Gray(RGB)", 2D) = "white"{}

	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	
	_RimFaceColor ("Rim Face Color", Color) = (0.26,0.19,0.16,0.0)
	_RimHairColor ("Rim Hair Color", Color) = (0.26,0.19,0.16,0.0)

	_RimFacePower ("Rim Face Power", Range(0.5,8.0)) = 3.0
	_RimHairPower ("Rim Hair Power", Range(0.5,8.0)) = 3.0

	_GrayFaceColor("Face Color", Color) = (0.5, 0.5, 0.5, 1)
	_GrayHairColor("Hair Color", Color) = (0.5, 0.5, 0.5, 1)
}
SubShader {
	LOD 300
	Tags { "RenderType"="Opaque" }

CGPROGRAM
#pragma surface surf BlinnPhong
sampler2D _MainTex;
samplerCUBE _Cube;
sampler2D _GrayTex;

fixed4 _Color;
fixed4 _ReflectColor;
half _Shininess;
float _RimFacePower;
float _RimHairPower;
float4 _RimFaceColor;
float4 _RimHairColor;

float4 _GrayFaceColor;
float4 _GrayHairColor;

struct Input {
	float2 uv_MainTex;
	float3 worldRefl;
	float3 viewDir;
	float2 uv_GrayTex;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex)* _Color;
	fixed4 graytex = tex2D(_GrayTex, IN.uv_GrayTex);

	fixed4 c = tex;
	
	fixed4 grayface = graytex * _GrayFaceColor;
	
	fixed4 grayhair = (1.0 - graytex.rgba) * _GrayHairColor;

	o.Albedo =c.rgb * grayhair.rgb;
	o.Albedo += c.rgb * grayface.rgb;

	o.Gloss = tex.a;
	o.Specular = _Shininess;
	
	fixed4 reflcol = texCUBE (_Cube, IN.worldRefl);
	reflcol *= tex.a;
	half rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));

	o.Emission = reflcol.rgb * _ReflectColor.rgb + _RimHairColor.rgb * pow (rim, _RimHairPower) * grayhair.rgb;
	o.Emission += reflcol.rgb * _ReflectColor.rgb + _RimFaceColor.rgb * pow (rim, _RimFacePower) * grayface.rgb;

	o.Alpha = reflcol.a * _ReflectColor.a;
}
ENDCG
}

FallBack "Reflective/VertexLit"
}
