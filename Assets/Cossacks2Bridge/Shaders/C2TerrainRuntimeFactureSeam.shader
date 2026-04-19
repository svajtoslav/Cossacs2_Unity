Shader "Cossacks2Bridge/TerrainRuntimeFactureSeam"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _SeamStrength("Seam Strength", Range(0,1)) = 0.42
        _SeamGamma("Seam Gamma", Range(0.25,4.0)) = 1.15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB

        Pass
        {
            Name "FactureSeam"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half _SeamStrength;
                half _SeamGamma;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv0        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4  color       : COLOR;
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
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half seamAlpha = saturate(i.color.a);
                seamAlpha = pow(seamAlpha, max(_SeamGamma, 0.001h));
                seamAlpha *= saturate(_SeamStrength);
                return half4(tex.rgb, seamAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
