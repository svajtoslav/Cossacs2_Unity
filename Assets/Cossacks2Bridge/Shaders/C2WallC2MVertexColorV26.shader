Shader "Cossacks2Bridge/WallC2MVertexColorV26"
{
    Properties
    {
        _MainTex ("C2M Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.01
        _UseVertexColor ("Use Vertex Color", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+450" "IgnoreProjector"="True" }
        Pass
        {
            // C2M/IMM wall models: opaque fixed-function-like pass.
            // Vertex diffuse color from Carcass is preserved; terrain can still occlude by depth.
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Offset -1, -1
            Cull [_Cull]
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _AlphaCutoff;
            fixed _UseVertexColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = lerp(fixed4(1,1,1,1), v.color, saturate(_UseVertexColor)) * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                clip(c.a - _AlphaCutoff);
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
