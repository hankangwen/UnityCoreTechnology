Shader "FastShadowReceiver/Receiver/Shadowmap/VisualizeReceiver" {
	Properties {
		_ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
		_DebugColor ("Visualize Color", Color) = (1,0,1,0.5)   	
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent-500" }
		Pass {
			Tags { "LightMode"="ForwardBase" }
			Cull Back
			ZWrite Off
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, -1

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#include "EnableCbuffer.cginc"
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#define _IS_SHADOW_ENABLED (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))

			struct appdata {
				float4 vertex   : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct v2f {
				float4 pos       : SV_POSITION;
				SHADOW_COORDS(0)
				UNITY_FOG_COORDS(1)
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert(appdata v)
			{
				v2f o;
#if defined(USING_STEREO_MATRICES) && defined(SHADOWS_SCREEN) && !defined(UNITY_NO_SCREENSPACE_SHADOWS)
				// TRANSFER_SHADOW macro does not calculate screenspace shadow correctly for stereo rendering.
				// use left eye screenspace for both eyes.
				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_SHADOW(o);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
#else
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				TRANSFER_SHADOW(o);
#endif
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}
			CBUFFER_START(UnityPerMaterial)
			fixed4 _ShadowColor;
			fixed4 _DebugColor;
			CBUFFER_END
			fixed4 frag(v2f i) : SV_Target
			{
			#if _IS_SHADOW_ENABLED
				fixed4 col = lerp(_ShadowColor, _DebugColor, SHADOW_ATTENUATION(i));
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
				return col;
			#else
				return _DebugColor;
			#endif
			}
			ENDHLSL
		}
	} 
	Fallback "FastShadowReceiver/BlendShadowMap"
}
