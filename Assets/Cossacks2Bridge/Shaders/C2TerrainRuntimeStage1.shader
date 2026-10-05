Shader "Cossacks2Bridge/TerrainRuntimeStage1"
{
    Properties
    {
        _LayerArray("Layer Array", 2DArray) = "" {}
        _LayerIndex("Layer Index", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Stage1Pass"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma require 2darray
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_LayerArray);
            SAMPLER(sampler_LayerArray);
            float _LayerIndex;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color       : COLOR;
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
                half4 tex = SAMPLE_TEXTURE2D_ARRAY(_LayerArray, sampler_LayerArray, frac(i.uv), _LayerIndex);
                half alpha = tex.a * i.color.a;
                return half4(tex.rgb * i.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
