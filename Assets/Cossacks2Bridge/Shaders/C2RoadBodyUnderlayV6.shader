Shader "Cossacks2Bridge/RoadBodyUnderlayV6"
{
    Properties
    {
        _MainTex ("Road Body / GroundTex", 2D) = "white" {}
        _BaseMap ("Road Body / GroundTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _RoadBodyOpacity ("Road Body Opacity", Float) = 0.92
        _RoadBodyColorBoost ("Road Body Color Boost", Float) = 1.06
        _UseTextureAlpha ("Use Texture Alpha", Float) = 0.0
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

            // Adapted replacement for original OneRoad::SurroundWithTexture().
            // Original mutates TexMap/FactureMap under the road. We keep terrain baked intact
            // and draw the same broad underlay as a separate transparent layer below road details.
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
            half _RoadBodyOpacity;
            half _RoadBodyColorBoost;
            half _UseTextureAlpha;

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
                half a = saturate(i.color.a * _RoadBodyOpacity * _Color.a * _BaseColor.a);
                a *= lerp(1.0h, tex.a, saturate(_UseTextureAlpha));
                half3 rgb = saturate(tex.rgb * i.color.rgb * _RoadBodyColorBoost) * _Color.rgb * _BaseColor.rgb;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack Off
}
