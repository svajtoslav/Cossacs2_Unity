Shader "Cossacks2Bridge/TerrainRuntimeFactureDot3"
{
    Properties
    {
        _MainTex("Facture Tex", 2D) = "white" {}
        _NormalTex("Normal Tex", 2D) = "bump" {}
        _BumpContrast("Bump Contrast", Float) = 0.6
        _BumpBrightness("Bump Brightness", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        ZTest LEqual
        Blend DstColor SrcColor

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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv0        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color        : COLOR;
                float2 uv          : TEXCOORD0;
            };

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
                const half tf = 128.0h / 255.0h;
                half3 nm = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, frac(i.uv)).xyz * 2.0h - 1.0h;
                half3 lts = i.color.rgb * 2.0h - 1.0h;
                half dotv = saturate(dot(normalize(nm), normalize(lts)));
                half3 diffTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frac(i.uv)).rgb;
                half alpha = i.color.a;
                half3 rgb = lerp(half3(tf, tf, tf), diffTex * dotv, alpha);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
