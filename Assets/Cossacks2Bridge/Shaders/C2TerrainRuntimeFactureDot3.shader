Shader "Cossacks2Bridge/TerrainRuntimeFactureDot3"
{
    Properties
    {
        _MainTex("Facture Tex", 2D) = "white" {}
        _NormalTex("Normal Tex", 2D) = "bump" {}
        _FactureTFactor("Facture TFactor", Color) = (0.5,0.5,0.5,0.5)
        _UseDitherLikeOriginal("Use Dither", Float) = 0
        _DitherStrengthLikeOriginal("Dither Strength", Range(0,1)) = 0
        _FactureCoverageSoftStartLikeAdapted("Coverage Soft Start", Range(0,1)) = 0.10039216
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Back
        ZWrite On
        ZTest LEqual
        Blend DstColor SrcColor
        ColorMask RGB

        Pass
        {
            Name "FactureDot3"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FactureTFactor;
                half _UseDitherLikeOriginal;
                half _DitherStrengthLikeOriginal;
                half _FactureCoverageSoftStartLikeAdapted;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color      : COLOR;
                float2 uv0       : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color        : COLOR;
                float2 uv          : TEXCOORD0;
            };

            float OrderedBayer4x4LikeOriginal(float2 pixelPos)
            {
                int x = ((int)floor(pixelPos.x)) & 3;
                int y = ((int)floor(pixelPos.y)) & 3;
                static const float kBayer[16] =
                {
                    0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                   12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                    3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                   15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
                };
                return kBayer[y * 4 + x];
            }

            half3 ApplyFactureDitherLikeOriginal(half3 rgb, float2 pixelPos)
            {
                if (_UseDitherLikeOriginal < 0.5h)
                    return rgb;

                float threshold = OrderedBayer4x4LikeOriginal(pixelPos) - 0.5;
                float3 levels = float3(31.0, 63.0, 31.0);
                float3 invLevels = 1.0 / levels;
                float3 dithered = saturate(rgb + threshold * invLevels * _DitherStrengthLikeOriginal);
                dithered = floor(dithered * levels + 0.5) / levels;
                return saturate((half3)dithered);
            }

            half SmoothStep01LikeAdapted(half v)
            {
                v = saturate(v);
                return v * v * (3.0h - 2.0h * v);
            }

            half ComputeFactureCoverageAlphaLikeAdapted(half rawAlpha)
            {
                half a = saturate(rawAlpha);
                half softStart = max(_FactureCoverageSoftStartLikeAdapted, 0.0001h);
                half edgeFade = SmoothStep01LikeAdapted(a / softStart);
                return saturate(a * edgeFade);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv0;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 normalTex = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv);
                half4 diffuseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                half3 nm = normalize(normalTex.xyz * 2.0h - 1.0h);
                half3 diff = normalize(i.color.rgb * 2.0h - 1.0h);
                half3 dot3Value = saturate(dot(nm, diff)).xxx;
                half3 modulated = diffuseTex.rgb * dot3Value;
                half coverageAlpha = ComputeFactureCoverageAlphaLikeAdapted(i.color.a);
                half3 finalColor = lerp(_FactureTFactor.rgb, modulated, coverageAlpha);
                finalColor = ApplyFactureDitherLikeOriginal(finalColor, i.positionHCS.xy);
                return half4(finalColor, coverageAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
