Shader "Cossacks2Bridge/TerrainRuntimeFacture3"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _BlendTex("Blend Tex", 2D) = "white" {}
        _FactureTFactor("Facture TFactor", Color) = (0.5, 0.5, 0.5, 0.5)
        _UseDitherLikeOriginal("Use Dither", Float) = 0
        _DitherStrengthLikeOriginal("Dither Strength", Range(0,1)) = 0
        _FactureAlphaRefLikeOriginal("Alpha Ref", Range(0,1)) = 0.015686275
        _FactureCoverageSoftStartLikeAdapted("Coverage Soft Start", Range(0,1)) = 0
        _PairBlendEnabledLikeAdapted("Pair Blend Enabled", Float) = 0
        _PairBlendThresholdLikeAdapted("Pair Blend Threshold", Range(0,1)) = 0.18
        _PairBlendStrengthLikeAdapted("Pair Blend Strength", Range(0,1)) = 0.90
        _PairBlendGammaLikeAdapted("Pair Blend Gamma", Range(0.25,4)) = 0.85
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend DstColor SrcColor
        ColorMask RGB

        Pass
        {
            Name "Facture3"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BlendTex);
            SAMPLER(sampler_BlendTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FactureTFactor;
                half _UseDitherLikeOriginal;
                half _DitherStrengthLikeOriginal;
                half _FactureAlphaRefLikeOriginal;
                half _FactureCoverageSoftStartLikeAdapted;
                half _PairBlendEnabledLikeAdapted;
                half _PairBlendThresholdLikeAdapted;
                half _PairBlendStrengthLikeAdapted;
                half _PairBlendGammaLikeAdapted;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float2 pairData   : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4  color       : COLOR;
                float2 uv          : TEXCOORD0;
                float2 uvBlend     : TEXCOORD1;
                float2 pairData    : TEXCOORD2;
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

            half ComputeFactureCoverageAlphaLikeAdapted(half rawAlpha)
            {
                return saturate(rawAlpha);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv0;
                o.uvBlend = v.uv1;
                o.pairData = v.pairData;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half rawAlpha = saturate(i.color.a);
                clip(rawAlpha - _FactureAlphaRefLikeOriginal);

                half coverageAlpha = ComputeFactureCoverageAlphaLikeAdapted(rawAlpha);
                clip(coverageAlpha - 0.0001h);

                if (_PairBlendEnabledLikeAdapted > 0.5h)
                {
                    half4 blendTex = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uvBlend);
                    half edgeFactor = saturate(i.pairData.x);
                    half blendMask = smoothstep(_PairBlendThresholdLikeAdapted, 1.0h, edgeFactor);
                    blendMask = pow(saturate(blendMask), max(_PairBlendGammaLikeAdapted, 0.0001h));
                    blendMask = saturate(blendMask * _PairBlendStrengthLikeAdapted);
                    tex.rgb = lerp(tex.rgb, blendTex.rgb, blendMask);
                }

                half3 blended = lerp(_FactureTFactor.rgb, tex.rgb, coverageAlpha);
                blended = ApplyFactureDitherLikeOriginal(blended, i.positionHCS.xy);
                return half4(blended, coverageAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
