Shader "Cossacks2Bridge/WallC2MDambaSolidRGBV34"
{
    Properties
    {
        _MainTex ("DAMBA WALLS.g16 RGB Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 8
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Geometry+600" "RenderType"="Opaque" "IgnoreProjector"="True" }

        Pass
        {
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            Offset -4, -4
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

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
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv);
                fixed3 vc = max(i.color.rgb, fixed3(1.0/255.0, 1.0/255.0, 1.0/255.0));
                fixed4 outc;
                outc.rgb = texel.rgb * _Color.rgb * vc;
                outc.a = 1.0;
                return outc;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
