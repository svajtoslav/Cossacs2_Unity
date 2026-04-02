Shader "Cossacks2Bridge/TerrainRuntimeVoidFill"
{
    Properties
    {
        _MainTex("Preview Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Geometry-100" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "VoidFill"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return tex * _Color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
