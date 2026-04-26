Shader "Cossacks2Bridge/TerrainFinalColorPolishV4"
{
    Properties
    {
        _MainTex ("Terrain Texture", 2D) = "white" {}
        _BaseMap ("Terrain Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _C2Warm ("C2 Warm RGB", Vector) = (1.055, 1.002, 0.962, 1)
        _C2Saturation ("C2 Saturation", Float) = 1.030
        _C2Contrast ("C2 Contrast", Float) = 1.030
        _C2ShadowWarm ("C2 Shadow Warm R,G,CoolB,LumaLimit", Vector) = (0.040, 0.006, 0.055, 0.60)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BaseMap;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _BaseColor;
            float4 _C2Warm;
            float _C2Saturation;
            float _C2Contrast;
            float4 _C2ShadowWarm;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv) * _Color * _BaseColor;

                float3 rgb = c.rgb;

                // Same tuned polish as V3, but on GPU.
                rgb *= _C2Warm.rgb;

                float gray = (rgb.r + rgb.g + rgb.b) * 0.33333334;
                rgb = float3(gray, gray, gray) + (rgb - float3(gray, gray, gray)) * _C2Saturation;

                rgb = (rgb - float3(0.5, 0.5, 0.5)) * _C2Contrast + float3(0.5, 0.5, 0.5);
                rgb = saturate(rgb);

                float luma = dot(rgb, float3(0.299, 0.587, 0.114));
                float darkness = saturate((_C2ShadowWarm.w - luma) / max(0.0001, _C2ShadowWarm.w));

                rgb.r *= 1.0 + darkness * _C2ShadowWarm.x;
                rgb.g *= 1.0 + darkness * _C2ShadowWarm.y;
                rgb.b *= 1.0 - darkness * _C2ShadowWarm.z;

                c.rgb = saturate(rgb);
                return c;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Texture"
}
