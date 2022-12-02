Shader "FastShadowReceiver/Projector/Visualize Receiver" {
	Properties {
		_Color ("Main Color", Color) = (1,0,1,0.5)   	
		[NoScaleOffset] _ShadowTex ("Cookie", 2D) = "gray" {}
		_ClipScale ("Near Clip Scale", Float) = 100
		_Alpha ("Shadow Darkness", Range (0, 1)) = 1.0
	}

	Subshader {
		Tags { "Queue"="Transparent-1" "IgnoreProjector"="True" }
		Pass {
			Stencil {
				Ref [P4LWRP_StencilRef]
				ReadMask [P4LWRP_StencilMask]
				Comp Equal
				Pass Zero
				ZFail Keep
			}
			ZWrite Off
			Fog { Color (1, 1, 1) }
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, -1
 
			HLSLPROGRAM
			#pragma vertex fsr_vert_projector_nearclip
			#pragma fragment fsr_frag_projector_shadow_visualize_nearclip
			#pragma shader_feature_local _ FSR_PROJECTOR_FOR_LWRP
			#pragma multi_compile_local _ FSR_RECEIVER
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#include "FastShadowReceiver.cginc"
			ENDHLSL
		}
	}
	CustomEditor "FastShadowReceiver.ProjectorShaderGUI"
}
