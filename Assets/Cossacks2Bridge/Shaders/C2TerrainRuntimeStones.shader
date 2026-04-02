Shader "Cossacks2Bridge/Stones"
{
    Properties
    {
        _MainTex("Texture0 GroundTex", 2D) = "white" {}
        _StoneTex("Texture1 kamni", 2D) = "white" {}
        _DiffuseColor("Diffuse Color", Color) = (1,1,1,1)
        _MainUvRect("Main UV Rect", Vector) = (0,0,1,1)
        _StoneUvRect("Stone UV Rect", Vector) = (0,0,1,1)
        _C2Rhw("RHW", Float) = 1
        _C2UseStrictTnL("Use strict XYZRHW-like path", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Stones"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_StoneTex);
            SAMPLER(sampler_StoneTex);

            float4 _C2Viewport;
            float4 _DiffuseColor;
            float4 _MainUvRect;
            float4 _StoneUvRect;
            float _C2Rhw;
            float _C2UseStrictTnL;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 diffuse      : COLOR;
                float2 uv0         : TEXCOORD0;
                float2 uv1         : TEXCOORD1;
            };

            float SafeReciprocalLikeOriginal(float v)
            {
                return abs(v) > 1e-6 ? rcp(v) : 1.0;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 screenPos = TransformObjectToWorld(v.positionOS.xyz).xyz;
                float clipW = SafeReciprocalLikeOriginal(_C2Rhw);
                float ndcX = ((screenPos.x - _C2Viewport.z) / max(_C2Viewport.x, 1.0)) * 2.0 - 1.0;
                float ndcY = 1.0 - ((screenPos.y - _C2Viewport.w) / max(_C2Viewport.y, 1.0)) * 2.0;
                float ndcZ = screenPos.z;
                o.positionHCS = float4(ndcX * clipW, ndcY * clipW, ndcZ * clipW, clipW);
                o.diffuse = _DiffuseColor;
                o.uv0 = _MainUvRect.xy + v.uv0 * _MainUvRect.zw;
                o.uv1 = _StoneUvRect.xy + v.uv0 * _StoneUvRect.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv0);
                half4 tex1 = SAMPLE_TEXTURE2D(_StoneTex, sampler_StoneTex, i.uv1);

                half3 stage0 = tex0.rgb * i.diffuse.rgb;
                half3 finalRgb = saturate(tex1.rgb * stage0 * 2.0h);
                half finalA = tex1.a;
                clip(finalA - (1.0h / 255.0h));
                return half4(finalRgb, finalA);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
