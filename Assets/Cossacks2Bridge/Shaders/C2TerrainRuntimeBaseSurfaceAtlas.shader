Shader "Cossacks2Bridge/TerrainRuntimeSurfaceBlendLikeOriginal"
{
    // Surface.xml literal no-Z contract still needs ordered full-pipeline parity.
    // This step removes base depth writes but keeps the working two-stage alpha semantics intact.
    Properties
    {
        _GroundAtlas("GroundTex.bmp", 2D) = "white" {}
        _CrossTex("BoundNew128.tga", 2D) = "white" {}
        _UseCrossLikeOriginal("Use BoundNew128", Float) = 1
        _UseOverlayLikeOriginal("Use overlay stage", Float) = 1
        _UseDitherLikeOriginal("Use Surface.xml dithering", Float) = 0
        _DitherStrengthLikeOriginal("Dither strength", Range(0,1)) = 0
        _SurfacePassModeLikeAdapted("Surface Pass Mode", Float) = 0
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
        float4 _GroundAtlas_TexelSize;
        float4 _CrossTex_TexelSize;
        float _UseCrossLikeOriginal;
        float _UseOverlayLikeOriginal;
        float _UseDitherLikeOriginal;
        float _DitherStrengthLikeOriginal;
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

        float2 SafeGroundAtlasUvLikeAdapted(float2 uv)
        {
            // Original-like atlas contract: sample direct atlas UVs without the extra safety clamp.
            return uv;
        }

        float2 SafeCrossUvLikeAdapted(float2 uv)
        {
            // Original-like BoundNew128 contract: wrap the authored UVs, but do not add Unity-only half-texel offsets.
            return frac(uv);
        }

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.color = v.color;
            o.uv0 = v.uv0;
            o.uv1 = v.uv1;
            o.uv2 = v.uv2;
            return o;
        }

        fixed3 SampleSurfaceRgbLikeAdapted(v2f i)
        {
            float2 atlasUv = SafeGroundAtlasUvLikeAdapted(i.uv0);
            fixed4 tileCol = tex2D(_GroundAtlas, atlasUv);
            return saturate(tileCol.rgb * i.color.rgb * 2.0);
        }

        fixed ComputeOverlayAlphaLikeAdapted(v2f i)
        {
            fixed diffuseAlpha = saturate(i.color.a);

            if (_UseCrossLikeOriginal > 0.5 && i.uv1.y > 0.5)
            {
                fixed4 crossCol = tex2D(_CrossTex, SafeCrossUvLikeAdapted(i.uv2));
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
        float OrderedBayer4x4LikeOriginal(float2 pixelPos)
        {
            float2 p = floor(pixelPos);
            float x = fmod(p.x, 4.0);
            float y = fmod(p.y, 4.0);

            if (y < 1.0)
            {
                if (x < 1.0) return 0.0 / 16.0;
                if (x < 2.0) return 8.0 / 16.0;
                if (x < 3.0) return 2.0 / 16.0;
                return 10.0 / 16.0;
            }
            if (y < 2.0)
            {
                if (x < 1.0) return 12.0 / 16.0;
                if (x < 2.0) return 4.0 / 16.0;
                if (x < 3.0) return 14.0 / 16.0;
                return 6.0 / 16.0;
            }
            if (y < 3.0)
            {
                if (x < 1.0) return 3.0 / 16.0;
                if (x < 2.0) return 11.0 / 16.0;
                if (x < 3.0) return 1.0 / 16.0;
                return 9.0 / 16.0;
            }

            if (x < 1.0) return 15.0 / 16.0;
            if (x < 2.0) return 7.0 / 16.0;
            if (x < 3.0) return 13.0 / 16.0;
            return 5.0 / 16.0;
        }

        fixed3 ApplySurfaceDitherLikeOriginal(fixed3 rgb, float2 pixelPos)
        {
            if (_UseDitherLikeOriginal <= 0.5 || _DitherStrengthLikeOriginal <= 0.0)
                return saturate(rgb);

            float threshold = OrderedBayer4x4LikeOriginal(pixelPos) - 0.5;

            float3 levels = float3(31.0, 63.0, 31.0);
            float3 invLevels = 1.0 / levels;
            float3 dithered = saturate(rgb + threshold * invLevels * _DitherStrengthLikeOriginal);
            dithered = floor(dithered * levels + 0.5) / levels;
            return saturate(dithered);
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

                fixed3 rgb = SampleSurfaceRgbLikeAdapted(i);
                rgb = ApplySurfaceDitherLikeOriginal(rgb, i.pos.xy);
                return fixed4(rgb, alpha);
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

                fixed3 rgb = SampleSurfaceRgbLikeAdapted(i);
                rgb = ApplySurfaceDitherLikeOriginal(rgb, i.pos.xy);
                fixed finalAlpha = ComputeOverlayAlphaLikeAdapted(i);

                clip(finalAlpha - (39.0 / 255.0));

                return fixed4(rgb, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
