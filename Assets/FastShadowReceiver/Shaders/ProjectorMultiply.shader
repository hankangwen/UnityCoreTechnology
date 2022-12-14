Shader "FastShadowReceiver/Projector/Multiply" {
	Properties {
		[NoScaleOffset] _ShadowTex ("Cookie", 2D) = "gray" {}
		[NoScaleOffset] _FalloffTex ("FallOff", 2D) = "white" {}
		_Alpha ("Shadow Darkness", Range (0, 1)) = 1.0
		_Offset ("Offset", Range (-10, 0)) = -1.0
		_OffsetSlope ("Offset Slope Factor", Range (-1, 0)) = -1.0
	}
	Subshader {
		Tags {"Queue"="Transparent-1" "IgnoreProjector"="True"}
		Pass {
			ZWrite Off
			Fog { Color (1, 1, 1) }
			ColorMask RGB
			Blend DstColor Zero
			Offset [_OffsetSlope], [_Offset]
 
			HLSLPROGRAM
			#pragma vertex fsr_vert_projector
			#pragma fragment fsr_frag_projector_shadow_falloff
			#pragma shader_feature_local _ FSR_PROJECTOR_FOR_LWRP
			#pragma multi_compile_local _ FSR_RECEIVER 
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#define FSR_USE_ALPHA
			#include "FastShadowReceiver.cginc"
			ENDHLSL
		}
	}
	Fallback "FastShadowReceiver/Projector/Multiply Without Falloff"
	CustomEditor "FastShadowReceiver.ProjectorShaderGUI"
}
