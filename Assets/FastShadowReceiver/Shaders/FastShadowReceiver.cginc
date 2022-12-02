//
// FastShadowReceiver.cginc
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

#if !defined(FAST_SHADOW_RECEIVER_CGINC_DEFINED)
#define FAST_SHADOW_RECEIVER_CGINC_DEFINED

#include "EnableCbuffer.cginc"
#if defined(FSR_USE_LIGHTMAP_ST)
#include "UnityShaderVariablesForSRP.cginc" // put unity_LightmapST into UnityPerDraw cbuffer
#endif
#include "UnityCG.cginc"

#ifdef UNITY_HDR_ON
#define FSR_LIGHTCOLOR4 half4
#define FSR_LIGHTCOLOR3 half3
#else
#define FSR_LIGHTCOLOR4 fixed4
#define FSR_LIGHTCOLOR3 fixed3
#endif

#if defined(FSR_PROJECTOR_FOR_LWRP)
FSR_LIGHTCOLOR4 _MainLightColor;
#define FSR_MAINLIGHTCOLOR	_MainLightColor
#else
fixed4 _LightColor0;
#define FSR_MAINLIGHTCOLOR	_LightColor0
#endif


#if defined(FSR_RECEIVER) // FSR_RECEIVER keyword is used by Projection Receiver Renderer component which is contained in Fast Shadow Receiver.

CBUFFER_START(FSR_ProjectorTransform)
float4x4 _FSRProjector;
float4 _FSRProjectDir;
CBUFFER_END

void fsrTransformVertex(float4 v, out float4 clipPos, out float4 shadowUV)
{
	clipPos = UnityObjectToClipPos(v);
	shadowUV = mul(_FSRProjector, v);
}
float3 fsrProjectorDir()
{
	return _FSRProjectDir.xyz;
}
#elif defined(FSR_PROJECTOR_FOR_LWRP)

CBUFFER_START(FSR_ProjectorTransform)
uniform float4x4 _FSRWorldToProjector;
uniform float4 _FSRWorldProjectDir;
CBUFFER_END

void fsrTransformVertex(float4 v, out float4 clipPos, out float4 shadowUV)
{
	float4 worldPos;
	worldPos.xyz = mul(unity_ObjectToWorld, v).xyz;
	worldPos.w = 1.0f;
	shadowUV = mul(_FSRWorldToProjector, worldPos);
#if defined(STEREO_CUBEMAP_RENDER_ON)
    worldPos.xyz += ODSOffset(worldPos.xyz, unity_HalfStereoSeparation.x);
#endif
	clipPos = mul(UNITY_MATRIX_VP, worldPos);
}
float3 fsrProjectorDir()
{
	return UnityWorldToObjectDir(_FSRWorldProjectDir.xyz);
}
#else

CBUFFER_START(FSR_ProjectorTransform)
float4x4 unity_Projector;
float4x4 unity_ProjectorClip;
CBUFFER_END

void fsrTransformVertex(float4 v, out float4 clipPos, out float4 shadowUV)
{
	clipPos = UnityObjectToClipPos(v);
	shadowUV = mul (unity_Projector, v);
	shadowUV.z = mul (unity_ProjectorClip, v).x;
}
float3 fsrProjectorDir()
{
	return normalize(float3(unity_Projector[2][0],unity_Projector[2][1], unity_Projector[2][2]));
}
#endif // FSR_RECEIVER

// define FSR_USE_XXXX macros before include FastShadowReceiver.cginc to put shader variables into UnityPerMaterial cbuffer
CBUFFER_START(UnityPerMaterial)
#if defined(FSR_USE_CLIPSCALE)
uniform float _ClipScale;
#endif
#if defined(FSR_USE_ALPHA)
uniform fixed _Alpha;
#endif
#if defined(FSR_USE_AMBIENT)
uniform fixed _Ambient;
#endif
#if defined(FSR_USE_COLOR)
uniform fixed4 _Color;
#endif
#if defined(FSR_USE_AMBIENTCOLOR)
uniform FSR_LIGHTCOLOR4 _AmbientColor;
#endif
#if defined(FSR_USE_SHADOWMASK)
uniform fixed4 _ShadowMaskSelector;
#endif
#if defined(FSR_EXTRA_VARIABLES)
FSR_EXTRA_VARIABLES;
#endif
CBUFFER_END

#if !defined(FSR_USE_CLIPSCALE)
uniform float _ClipScale;
#endif
#if !defined(FSR_USE_ALPHA)
uniform fixed _Alpha;
#endif
#if !defined(FSR_USE_AMBIENT)
uniform fixed _Ambient;
#endif
#if !defined(FSR_USE_COLOR)
uniform fixed4 _Color;
#endif
#if !defined(FSR_USE_AMBIENTCOLOR)
uniform FSR_LIGHTCOLOR4 _AmbientColor;
#endif
#if !defined(FSR_USE_SHADOWMASK)
uniform fixed4 _ShadowMaskSelector;
#endif

#define FSR_USE_CLIPSCALE
#define FSR_USE_ALPHA
#define FSR_USE_AMBIENT
#define FSR_USE_COLOR
#define FSR_USE_AMBIENTCOLOR
#define FSR_USE_SHADOWMASK

struct FSR_PROJECTOR_VERTEX_V {
	float4 vertex : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FSR_PROJECTOR_VERTEX_VN {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FSR_PROJECTOR_VERTEX_VNT1 {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 uv     : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FSR_V2F_PROJECTOR {
	float4 uv        : TEXCOORD0;
	UNITY_FOG_COORDS(1)
	float4 pos       : SV_POSITION;
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FSR_V2F_PROJECTOR_ALPHA {
	float4 uv        : TEXCOORD0;
	fixed  alpha     : TEXCOORD1;
	UNITY_FOG_COORDS(2)
	float4 pos       : SV_POSITION;
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FSR_V2F_PROJECTOR_NEARCLIP {
	float4 uv        : TEXCOORD0;
	UNITY_FOG_COORDS(1)
	half   alpha     : COLOR;
	float4 pos       : SV_POSITION;
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FSR_V2F_PROJECTOR_NEARCLIP_ALPHA {
	float4 uv        : TEXCOORD0;
	UNITY_FOG_COORDS(1)
	half2  alpha     : COLOR;
	float4 pos       : SV_POSITION;
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FSR_V2F_PROJECTOR_ALPHA_LIGHTMAPUV {
	float4 uv        : TEXCOORD0;
	half3  uv_alpha  : TEXCOORD1;
	UNITY_FOG_COORDS(2)
	float4 pos       : SV_POSITION;
	UNITY_VERTEX_OUTPUT_STEREO
};

struct FSR_V2F_PROJECTOR_NEARCLIP_ALPHA_LIGHTMAPUV {
	float4 uv        : TEXCOORD0;
	half4  uv_alpha  : TEXCOORD1;
	UNITY_FOG_COORDS(2)
	float4 pos       : SV_POSITION;
	UNITY_VERTEX_OUTPUT_STEREO
};

#if UNITY_VERSION < 201800 && !defined(SHADOWS_SHADOWMASK)
UNITY_DECLARE_TEX2D(unity_ShadowMask);
#endif

#define fsr_LightmapST unity_LightmapST
#define fsr_Lightmap   unity_Lightmap
#define fsr_ShadowMask unity_ShadowMask

uniform sampler2D _ShadowTex;
uniform sampler2D _FalloffTex;

FSR_V2F_PROJECTOR fsr_vert_projector(FSR_PROJECTOR_VERTEX_V v)
{
	FSR_V2F_PROJECTOR o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	fsrTransformVertex(v.vertex, o.pos, o.uv);
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

FSR_V2F_PROJECTOR_ALPHA fsr_vert_projector_diffuse(FSR_PROJECTOR_VERTEX_VN v)
{
	FSR_V2F_PROJECTOR_ALPHA o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	fsrTransformVertex(v.vertex, o.pos, o.uv);
	fixed diffuse = -dot(v.normal, fsrProjectorDir());
	o.alpha = _Alpha * diffuse/(saturate(diffuse) + _Ambient);
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

FSR_V2F_PROJECTOR_NEARCLIP fsr_vert_projector_nearclip(FSR_PROJECTOR_VERTEX_V v)
{
	FSR_V2F_PROJECTOR_NEARCLIP o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	fsrTransformVertex(v.vertex, o.pos, o.uv);
	o.alpha = _ClipScale*o.uv.z;
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

FSR_V2F_PROJECTOR_NEARCLIP_ALPHA fsr_vert_projector_diffuse_nearclip(FSR_PROJECTOR_VERTEX_VN v)
{
	FSR_V2F_PROJECTOR_NEARCLIP_ALPHA o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	fsrTransformVertex(v.vertex, o.pos, o.uv);
	o.alpha.x = _ClipScale*o.uv.z;
	half diffuse = -dot(v.normal, fsrProjectorDir());
	o.alpha.y = _Alpha * diffuse/(saturate(diffuse) + _Ambient);
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

FSR_V2F_PROJECTOR_ALPHA_LIGHTMAPUV fsr_vert_projector_diffuse_lightmap(FSR_PROJECTOR_VERTEX_VNT1 v)
{
	FSR_V2F_PROJECTOR_ALPHA_LIGHTMAPUV o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	fsrTransformVertex(v.vertex, o.pos, o.uv);
	fixed diffuse = -dot(v.normal, fsrProjectorDir());
	o.uv_alpha.xy = v.uv * fsr_LightmapST.xy + fsr_LightmapST.zw;
	o.uv_alpha.z = diffuse;
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

FSR_V2F_PROJECTOR_NEARCLIP_ALPHA_LIGHTMAPUV fsr_vert_projector_diffuse_nearclip_lightmap(FSR_PROJECTOR_VERTEX_VNT1 v)
{
	FSR_V2F_PROJECTOR_NEARCLIP_ALPHA_LIGHTMAPUV o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	fsrTransformVertex(v.vertex, o.pos, o.uv);
	o.uv_alpha.xy = v.uv * fsr_LightmapST.xy + fsr_LightmapST.zw;
	o.uv_alpha.z = _ClipScale*o.uv.z;
	half diffuse = -dot(v.normal, fsrProjectorDir());
	o.uv_alpha.w = diffuse;
	UNITY_TRANSFER_FOG(o, o.pos);
	return o;
}

fixed4 fsr_frag_projector_shadow_falloff(FSR_V2F_PROJECTOR i) : SV_Target
{
	fixed4 col;
	fixed alpha = tex2D(_FalloffTex, i.uv.zz).a;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	col.rgb = lerp(fixed3(1,1,1), col.rgb, _Alpha*alpha);
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_alpha_falloff(FSR_V2F_PROJECTOR_ALPHA i) : SV_Target
{
	fixed4 col;
	fixed alpha = tex2D(_FalloffTex, i.uv.zz).a;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	col.rgb = lerp(fixed3(1,1,1), col.rgb, saturate(i.alpha*alpha));
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_nearclip(FSR_V2F_PROJECTOR_NEARCLIP i) : SV_Target
{
	fixed4 col;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	col.rgb = lerp(fixed3(1,1,1), col.rgb, _Alpha*saturate(i.alpha));
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_alpha_nearclip(FSR_V2F_PROJECTOR_NEARCLIP_ALPHA i) : SV_Target
{
	fixed4 col;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	col.rgb = lerp(fixed3(1,1,1), col.rgb, saturate(saturate(i.alpha.x)*i.alpha.y));
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed fsrGetShadowMask(float2 uv)
{
	fixed4 shadowmask = UNITY_SAMPLE_TEX2D(fsr_ShadowMask, uv);
	//return shadowmask.r;
	return saturate(dot(shadowmask, _ShadowMaskSelector));
}

fixed4 fsr_frag_projector_shadow_alpha_falloff_shadowmask(FSR_V2F_PROJECTOR_ALPHA_LIGHTMAPUV i) : SV_Target
{
	fixed4 col;
	fixed alpha = tex2D(_FalloffTex, i.uv.zz).a;
	fixed shadowmask = fsrGetShadowMask(i.uv_alpha.xy);
	fixed diffuse = saturate(shadowmask * i.uv_alpha.z);
	shadowmask = _Alpha * diffuse/(diffuse + _Ambient);
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	col.rgb = lerp(fixed3(1,1,1), col.rgb, saturate(shadowmask*alpha));
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_alpha_nearclip_shadowmask(FSR_V2F_PROJECTOR_NEARCLIP_ALPHA_LIGHTMAPUV i) : SV_Target
{
	fixed4 col;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	fixed shadowmask = fsrGetShadowMask(i.uv_alpha.xy);
	fixed diffuse = saturate(shadowmask * i.uv_alpha.w);
	shadowmask = _Alpha * diffuse/(diffuse + _Ambient);
	col.a = 1.0f;
	col.rgb = lerp(fixed3(1,1,1), col.rgb, saturate(saturate(i.uv_alpha.z)*shadowmask));
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed3 fsrCalculateSubtractiveShadow(fixed3 shadowColor, half2 lightmapUV, half ndotl, fixed falloff)
{
	half4 bakedColorTex = UNITY_SAMPLE_TEX2D(fsr_Lightmap, lightmapUV);
	FSR_LIGHTCOLOR3 bakedColor = DecodeLightmap(bakedColorTex);
	FSR_LIGHTCOLOR3 mainLight = FSR_MAINLIGHTCOLOR * saturate(ndotl);
	FSR_LIGHTCOLOR3 ambientColor = max(bakedColor - mainLight, _AmbientColor.rgb);
	shadowColor = lerp(fixed3(1,1,1), shadowColor, saturate(_Alpha * falloff));
	return saturate((shadowColor * (bakedColor - ambientColor) + ambientColor) / bakedColor);
}

fixed3 fsrCalculateShadowmaskShadow(fixed3 shadowColor, half2 lightmapUV, half ndotl, fixed falloff)
{
	fixed shadowmask = fsrGetShadowMask(lightmapUV);
	half4 bakedColorTex = UNITY_SAMPLE_TEX2D(fsr_Lightmap, lightmapUV);
	FSR_LIGHTCOLOR3 bakedColor = DecodeLightmap(bakedColorTex);
	FSR_LIGHTCOLOR3 mainLight = FSR_MAINLIGHTCOLOR * saturate(shadowmask*ndotl);
	fixed3 shadow = saturate((_Alpha * falloff * mainLight)/(bakedColor + mainLight));
	return lerp(fixed3(1,1,1), shadowColor, shadow);
}

fixed4 fsr_frag_projector_shadow_alpha_falloff_lightmap_subtractive(FSR_V2F_PROJECTOR_ALPHA_LIGHTMAPUV i) : SV_Target
{
	fixed4 col;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	fixed alpha = tex2D(_FalloffTex, i.uv.zz).a;
	col.rgb = fsrCalculateSubtractiveShadow(col.rgb, i.uv_alpha.xy, i.uv_alpha.z, alpha);

	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_alpha_nearclip_lightmap_subtractive(FSR_V2F_PROJECTOR_NEARCLIP_ALPHA_LIGHTMAPUV i) : SV_Target
{
	fixed4 col;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	col.rgb = fsrCalculateSubtractiveShadow(col.rgb, i.uv_alpha.xy, i.uv_alpha.w, saturate(i.uv_alpha.z));

	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_alpha_falloff_lightmap_shadowmask(FSR_V2F_PROJECTOR_ALPHA_LIGHTMAPUV i) : SV_Target
{
	fixed4 col;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	fixed alpha = tex2D(_FalloffTex, i.uv.zz).a;
	col.rgb = fsrCalculateShadowmaskShadow(col.rgb, i.uv_alpha.xy, i.uv_alpha.z, alpha);

	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_alpha_nearclip_lightmap_shadowmask(FSR_V2F_PROJECTOR_NEARCLIP_ALPHA_LIGHTMAPUV i) : SV_Target
{
	fixed4 col;
	col.rgb = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv)).rgb;
	col.a = 1.0f;
	col.rgb = fsrCalculateShadowmaskShadow(col.rgb, i.uv_alpha.xy, i.uv_alpha.w, saturate(i.uv_alpha.z));

	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}

fixed4 fsr_frag_projector_shadow_visualize_nearclip(FSR_V2F_PROJECTOR_NEARCLIP i) : SV_Target
{
	fixed4 col = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uv));
	col = lerp(_Color, fixed4(0,0,0,1), saturate(i.alpha)*_Alpha*(1.0f - col));
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
	return col;
}


#endif // !defined(FAST_SHADOW_RECEIVER_CGINC_DEFINED)
