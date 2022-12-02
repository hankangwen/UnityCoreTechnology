Shader "FastShadowReceiver/Receiver/Gizmo" {
    Properties { _Color ("Main Color", Color) = (0,1,0,0.5) }
    SubShader {
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off Fog { Mode Off }
        Offset -1, -1
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "EnableCbuffer.cginc"
            #include "UnityCG.cginc"
			CBUFFER_START(UnityPerMaterial)
            fixed4 _Color;
			CBUFFER_END
            float4 vert (float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag () : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
