Shader "Cossacks2Bridge/TerrainRuntimeFactureBlendWedge"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _BlendStrength("Blend Strength", Range(0,1)) = 1
        _BlendGamma("Blend Gamma", Range(0.5,3.0)) = 1.7
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend DstColor OneMinusSrcAlpha
        ColorMask RGB

        Pass
        {
            Name "FactureBlendWedge"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half _BlendStrength;
                half _BlendGamma;
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
                half3 texRgb = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                half a = saturate(i.color.a * _BlendStrength);
                a = pow(a, _BlendGamma);
                return half4(texRgb * a, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
