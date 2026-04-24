Shader "Cossacks2Bridge/FastRuntimeSurfaceAtlasV8"
{
    Properties
    {
        _GroundAtlas("GroundTex.bmp", 2D) = "white" {}
        _CrossTex("BoundNew128.tga", 2D) = "white" {}
        _UseCrossLikeOriginal("Use BoundNew128", Float) = 1
        _UseOverlayLikeOriginal("Use overlay stage", Float) = 1
        _SurfacePassModeLikeAdapted("Surface Pass Mode", Float) = 0
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Transparent" }
        Cull Off

        CGINCLUDE
        #pragma target 3.0
        #include "UnityCG.cginc"

        sampler2D _GroundAtlas;
        sampler2D _CrossTex;
        fixed4 _Color;
        float _UseCrossLikeOriginal;
        float _UseOverlayLikeOriginal;
        float _SurfacePassModeLikeAdapted;

        struct appdata
        {
            float4 vertex : POSITION;
            fixed4 color : COLOR;
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float2 uv2 : TEXCOORD2;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            fixed4 color : COLOR;
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float2 uv2 : TEXCOORD2;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.color = v.color * _Color;
            o.uv0 = v.uv0;
            o.uv1 = v.uv1;
            o.uv2 = v.uv2;
            return o;
        }

        fixed3 SampleSurfaceRgbLikeAdapted(v2f i)
        {
            fixed4 tileCol = tex2D(_GroundAtlas, i.uv0);
            return saturate(tileCol.rgb * i.color.rgb * 2.0);
        }

        fixed ComputeOverlayAlphaLikeAdapted(v2f i)
        {
            fixed diffuseAlpha = saturate(i.color.a);
            if (_UseCrossLikeOriginal > 0.5 && i.uv1.y > 0.5)
            {
                fixed4 crossCol = tex2D(_CrossTex, frac(i.uv2));
                return saturate(crossCol.a + diffuseAlpha - 0.5);
            }
            return diffuseAlpha;
        }

        bool IsOverlayStageLikeAdapted(v2f i)
        {
            return i.uv1.x > 0.5 && _UseOverlayLikeOriginal > 0.5;
        }

        bool IsBaseOnlySurfacePassLikeAdapted()
        {
            return _SurfacePassModeLikeAdapted > 0.5 && _SurfacePassModeLikeAdapted < 1.5;
        }

        bool IsOverlayOnlySurfacePassLikeAdapted()
        {
            return _SurfacePassModeLikeAdapted > 1.5;
        }
        ENDCG

        Pass
        {
            ZWrite On
            ZTest LEqual
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBase

            fixed4 fragBase(v2f i) : SV_Target
            {
                if (IsOverlayOnlySurfacePassLikeAdapted() || IsOverlayStageLikeAdapted(i))
                    clip(-1);
                fixed alpha = saturate(i.color.a);
                clip(alpha - (39.0 / 255.0));
                return fixed4(SampleSurfaceRgbLikeAdapted(i), alpha);
            }
            ENDCG
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragOverlay

            fixed4 fragOverlay(v2f i) : SV_Target
            {
                if (IsBaseOnlySurfacePassLikeAdapted() || !IsOverlayStageLikeAdapted(i))
                    clip(-1);
                fixed finalAlpha = ComputeOverlayAlphaLikeAdapted(i);
                clip(finalAlpha - (39.0 / 255.0));
                return fixed4(SampleSurfaceRgbLikeAdapted(i), finalAlpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
