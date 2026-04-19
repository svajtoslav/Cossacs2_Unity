Shader "Cossacks2Bridge/TerrainRuntimeFacturePairBlend"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _BlendTex("Blend Tex", 2D) = "white" {}
        _BlendWidthLikeAdapted("Blend Width", Range(0.05,1)) = 0.34
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
            Name "FacturePairBlend"
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
                half _BlendWidthLikeAdapted;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half alpha         : TEXCOORD2;
                float2 uv0         : TEXCOORD0;
                float2 uv1         : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.alpha = saturate(v.color.a);
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 texMain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv0).rgb;
                half3 texBlend = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv1).rgb;
                half width = max(_BlendWidthLikeAdapted, 0.01h);
                half t = smoothstep(0.0h, width, saturate(i.alpha));
                half3 mixed = lerp(texBlend, texMain, t);
                return half4(mixed, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
