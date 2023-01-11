Shader "FX/Flare"   //Shader的名字
{
    Properties
    {
        //输入材质
        _MainTex ("Particle Texture", 2D) = "black" {}
    }
    SubShader
    {
        //Tags可以告诉硬件应该什么时候调用你的Shader，可以根据情况设置很多参数
        Tags { 
            "Queue" = "Transparent"     //Queue表示的是渲染队列
            "IgnoreProjector" = "True"  
            "RenderType"="Transparent"  //渲染类型
            "PreviewType"="Plane" 
            }
        //接下来是针对模型的设置，比如模型的双面显示、深度缓存关闭等
        Cull Off Lighting Off ZWrite Off Ztest Always Fog { Mode Off }
        
        Blend One One
        Pass    //为GPU定义pass通道，pass通道中定义了顶点着色器和片段着色器
        {
            CGPROGRAM   //表示CG程序，和END是成对出现的，它们之间的代码是要给GPU处理的
            #pragma vertex vert
            #pragma fragment frag
            //引用Unity自己的Shader库，因为下面函数中会调用库函数，比如：tex2D、mul等。
            #include "UnityCG.cginc" 
            
            //下面三个是变量的声明。
            //其中sampler2D定义的变量名字与Properties中定义的变量名字是一样的，表示外面传进来的图片。
            sampler2D _MainTex;
            fixed4 _TintColor;
            float4 _MainTex_ST;

            //定义两个结构体，分别表示顶点着色器的参数和片段着色器的参数
            struct appdata_t
            {
                float4 vertex : POSITION;   //顶点位置
                float4 color : COLOR;       //颜色
                float2 texcoord : TEXCOORD0;//纹理坐标
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;//顶点位置
                float4 color : COLOR;       //颜色
                float2 texcoord : TEXCOORD0;//纹理坐标
            };

            v2f vert (appdata_t v)  //顶点着色器，输入是顶点着色器的结构体appdata_t
            {
                v2f o;  //将得到的结果返回给片段着色器,输出是片段着色器结构体
                o.vertex = UnityObjectToClipPos(v.vertex);//对顶点进行模型、世界、投影变换（MVP）
                o.color = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target //片段着色器，输入是片段着色器的结构体v2f
            {
                fixed4 col;
                fixed4 tex = tex2D(_MainTex, i.texcoord);   //对输入的纹理根据纹理坐标采样
                //region 将获得到的颜色通过下面的公式计算出，最终将返回值输出到屏幕上，并显示出来
                col.rgb = i.color.rgb * tex.rgb;    
                col.a = tex.a;
                //endregion
                return col;
            }
            ENDCG
        }
    }
}
