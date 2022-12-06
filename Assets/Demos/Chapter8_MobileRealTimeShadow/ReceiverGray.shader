// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*这个Shader实现了透明材质的渲染*/
Shader "Custom/Transparent/ReceiverGray"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _Cutoff ("Base Alpha cutoff", Range (.0, .9)) = .2
    }
    
    SubShader 
    {
        Tags { "Queue"="Transparent" }
        
        Lighting Off
        ZWrite Off
        // 双面显示打开
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        // pass 通道
        Pass 
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            // 片段着色器
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            // 像素着色器
            float4 _Color;
            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.texcoord);
                if(col.a < _Cutoff)
                {
                    clip(col.a - _Cutoff);
                }
                else
                {
                    col.rgb = col.rgb * float3(0,0,0);
                    col.rgb = col.rgb + _Color;
                    col.a = _Color.a;
                }
                return col;
            }
            
            ENDCG
        }
    }
}
