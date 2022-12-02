Shader "FastShadowReceiver/Quad Shadow/Multiply Solid" {
	Properties {
		[NoScaleOffset] _ShadowTex ("Packed Cookie", 2D) = "gray" {}
		_AlphaTest ("Alpha Test", Range (0, 1)) = 0.5
	}
	Subshader {
		Tags {"Queue"="Transparent-1" "IgnoreProjector"="True"}
		Pass {
			ZWrite Off
			Fog { Color (1, 1, 1) }
			ColorMask RGB
			Blend DstColor Zero
			Offset -1, -1
			Stencil {
				Ref 1
				Comp NotEqual
				Pass Replace
				ZFail Keep
			}

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#include "EnableCbuffer.cginc"
			#include "UnityCG.cginc"
			
			struct appdata {
				float4 vertex   : POSITION;
				fixed4 color    : COLOR;
				float4 texcoord : TEXCOORD;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float2 uv     : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				fixed  alpha  : COLOR;
				float4 pos    : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			v2f vert(appdata v)
			{
				v2f o;
			    UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv  = v.texcoord.xy;
				o.alpha = v.color.a;
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}
			
			sampler2D _ShadowTex;
			fixed _AlphaTest;
			
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col;
				col = tex2D(_ShadowTex, i.uv);
				clip(col.a - _AlphaTest);
				col.a = 1.0f;
				col.rgb = lerp(fixed3(1,1,1), col.rgb, i.alpha);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1));
				return col;
			}
			ENDHLSL
		}
	}
}
