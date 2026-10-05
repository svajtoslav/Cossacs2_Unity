Shader "Cossacks2Bridge/RoadLayerV4"
{
    Properties
    {
        _MainTex ("Road Texture", 2D) = "white" {}
        _BaseMap ("Road Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _RoadColorBoost ("Road Color Boost", Float) = 2.0
        _RoadAlphaBoost ("Road Alpha Boost", Float) = 1.35
        _RoadMinTextureAlpha ("Road Min Texture Alpha", Float) = 0.72
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _BaseColor;
            half _RoadColorBoost;
            half _RoadAlphaBoost;
            half _RoadMinTextureAlpha;

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
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Original road shader behaves like fixed-function modulate2x with vertex diffuse.
                half3 rgb = saturate(tex.rgb * i.color.rgb * _RoadColorBoost) * _Color.rgb * _BaseColor.rgb;

                // Old road TGA assets may have weak/unused alpha. Diffuse alpha is the real road mask.
                half texA = max(tex.a, _RoadMinTextureAlpha);
                half a = saturate(i.color.a * texA * _RoadAlphaBoost * _Color.a * _BaseColor.a);

                return fixed4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
