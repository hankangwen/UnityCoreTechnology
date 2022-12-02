Shader "FastShadowReceiver/Projector/Light x Shadow" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		[NoScaleOffset] _LightTex ("Cookie", 2D) = "" {}
		[NoScaleOffset] _FalloffTex ("FallOff", 2D) = "" {}
		_Offset ("Offset", Range (-10, 0)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (-1, 0)) = -1.0
	}
	 
	Subshader {
		Tags {"Queue"="Transparent-1" "FSRMaxShadowNum"="3" "IgnoreProjector"="True"}
		Pass {
			ZWrite Off
			Fog { Color (0, 0, 0) }
			ColorMask RGB
			Blend DstColor One
			Offset [_OffsetSlope], [_Offset]
	 
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local FSR_SHADOW_PROJECTOR_COUNT_0 FSR_SHADOW_PROJECTOR_COUNT_1 FSR_SHADOW_PROJECTOR_COUNT_2 FSR_SHADOW_PROJECTOR_COUNT_3
			#pragma shader_feature_local _ FSR_PROJECTOR_FOR_LWRP
			#pragma multi_compile_local _ FSR_RECEIVER 
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#define FSR_USE_COLOR
			#include "FastShadowReceiver.cginc"
			#if defined(FSR_SHADOW_PROJECTOR_COUNT_1)
				#define FSR_SHADOW_NUM 1
			#elif defined(FSR_SHADOW_PROJECTOR_COUNT_2)
				#define FSR_SHADOW_NUM 2
			#elif defined(FSR_SHADOW_PROJECTOR_COUNT_3)
				#define FSR_SHADOW_NUM 3
			#else
				#define FSR_SHADOW_NUM 0
			#endif
			struct appdata {
				float4 vertex   : POSITION;
				float3 normal   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 uvLight : TEXCOORD0;
				#if 0 < FSR_SHADOW_NUM
					float4 uvShadow1 : TEXCOORD1;
				#endif
				#if 1 < FSR_SHADOW_NUM
					float4 uvShadow2 : TEXCOORD2;
				#endif
				#if 2 < FSR_SHADOW_NUM
					float4 uvShadow3 : TEXCOORD3;
				#endif
				UNITY_FOG_COORDS(4)
				fixed  alpha  : COLOR;
				float4 pos : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			float4x4 _ShadowProjector1;
			float4x4 _ShadowProjector2;
			float4x4 _ShadowProjector3;
			
			v2f vert (appdata v)
			{
				v2f o;
			    UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				fsrTransformVertex(v.vertex, o.pos, o.uvLight);
				o.alpha = -dot(v.normal, fsrProjectorDir());
				#if 0 < FSR_SHADOW_NUM
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uvShadow1 = mul (_ShadowProjector1, worldPos);
				#endif
				#if 1 < FSR_SHADOW_NUM
				o.uvShadow2 = mul (_ShadowProjector2, worldPos);
				#endif
				#if 2 < FSR_SHADOW_NUM
				o.uvShadow3 = mul (_ShadowProjector3, worldPos);
				#endif
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}
			
			sampler2D _LightTex;
			sampler2D _ShadowTex1;
			sampler2D _ShadowTex2;
			sampler2D _ShadowTex3;
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2Dproj (_LightTex, UNITY_PROJ_COORD(i.uvLight));
				fixed alpha = i.alpha * tex2D (_FalloffTex, i.uvLight.zz).a;
				#if 0 < FSR_SHADOW_NUM
				col.rgb *= lerp(fixed3(1,1,1), tex2Dproj(_ShadowTex1, UNITY_PROJ_COORD(i.uvShadow1)).rgb, saturate(i.uvShadow1.z));
				#endif
				#if 1 < FSR_SHADOW_NUM
				col.rgb *= lerp(fixed3(1,1,1), tex2Dproj(_ShadowTex2, UNITY_PROJ_COORD(i.uvShadow2)).rgb, saturate(i.uvShadow2.z));
				#endif
				#if 2 < FSR_SHADOW_NUM
				col.rgb *= lerp(fixed3(1,1,1), tex2Dproj(_ShadowTex3, UNITY_PROJ_COORD(i.uvShadow3)).rgb, saturate(i.uvShadow3.z));
				#endif
				col.rgb *= alpha * _Color.rgb;
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0));
				return col;
			}
			ENDHLSL
		}
	}
	CustomEditor "FastShadowReceiver.ProjectorShaderGUI"
}
