Shader "Cossacks2Bridge/TerrainRuntimeOverlay"
{
    Properties
    {
        _LayerArray("Layer Array", 2DArray) = "" {}
        _LayerIndex("Layer Index", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "OverlayPass"
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
                float2 uv1        : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color        : COLOR;
                float2 uv          : TEXCOORD0;
                float2 uvCross     : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv0;
                o.uvCross = v.uv1;
                return o;
            }

            float ComputeCrossMaskFromUv2(float2 uvCross)
            {
                float2 atlas = uvCross * 2.0;
                float2 cell = frac(atlas);
                float2 quad = floor(atlas);
                float orient = quad.x + quad.y * 2.0;
                float d;
                if (orient < 0.5)
                    d = cell.x + cell.y;
                else if (orient < 1.5)
                    d = (1.0 - cell.x) + cell.y;
                else if (orient < 2.5)
                    d = cell.x + (1.0 - cell.y);
                else
                    d = (1.0 - cell.x) + (1.0 - cell.y);
                return saturate(d);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D_ARRAY(_LayerArray, sampler_LayerArray, frac(i.uv), _LayerIndex);
                half cross = ComputeCrossMaskFromUv2(i.uvCross);
                half alpha = tex.a * i.color.a * cross;
                return half4(tex.rgb * i.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
