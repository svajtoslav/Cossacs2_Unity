Shader "Cossacks2Bridge/TerrainTriLiteralPreview"
{
    Properties
    {
        _LayerArray("Layer Array", 2DArray) = "" {}
        _StageAlphaScale("Stage Alpha Scale", Float) = 1
        _OverlayMode("Overlay Mode", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1
        _Brightness("Brightness", Range(0,2)) = 1.08
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "TerrainTriLiteralPreview"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma require 2darray
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_LayerArray);
            SAMPLER(sampler_LayerArray);

            float _StageAlphaScale;
            float _Brightness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color       : COLOR;
                float2 uv0        : TEXCOORD0;
                float4 uv3        : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color        : COLOR;
                float2 uv0         : TEXCOORD0;
                float layer        : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv0 = v.uv0;
                o.layer = v.uv3.x;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D_ARRAY(_LayerArray, sampler_LayerArray, frac(i.uv0), i.layer);
                half3 lit = texel.rgb * i.color.rgb * _Brightness;
                half alpha = saturate(texel.a * i.color.a * _StageAlphaScale);
                if (alpha <= 0.001h)
                    clip(-1);
                return half4(lit, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
