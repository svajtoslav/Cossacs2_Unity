Shader "Cossacks2Bridge/TerrainRuntimeFacture3SurfacePass"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
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
            Name "Facture3SurfacePass"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend DstColor SrcColor, One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _C2Viewport;
            float _C2UseStrictTnL;

            struct Attributes
            {
                float4 positionOS : POSITION; // pretransformed screen x/y + ndc z
                float4 color      : COLOR;    // diffuse alpha carries facture weight
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float4 uv2        : TEXCOORD2; // x = rhw
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 diffuse      : COLOR;
                float2 uv0         : TEXCOORD0;
            };

            float SafeReciprocalLikeOriginal(float v)
            {
                return abs(v) > 1e-6 ? rcp(v) : 1.0;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                float clipW = SafeReciprocalLikeOriginal(v.uv2.x);
                float ndcX = ((v.positionOS.x - _C2Viewport.z) / max(_C2Viewport.x, 1.0)) * 2.0 - 1.0;
                float ndcY = 1.0 - ((v.positionOS.y - _C2Viewport.w) / max(_C2Viewport.y, 1.0)) * 2.0;
                float ndcZ = v.positionOS.z;

                o.positionHCS = float4(ndcX * clipW, ndcY * clipW, ndcZ * clipW, clipW);
                o.diffuse = v.color;
                o.uv0 = v.uv0;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frac(i.uv0));

                // Facture3.xml:
                // ColorOp   = BlendDiffuseAlpha(Texture, TFactor)
                // ColorArg1 = Texture
                // ColorArg2 = TFactor (0x80808080)
                // AlphaOp   = SelectArg1(Diffuse)
                const half tf = (128.0h / 255.0h);
                half alpha = saturate(i.diffuse.a);

                // AlphaTestEnable=True, AlphaRef=4
                clip(alpha - (4.0h / 255.0h));

                half3 rgb = lerp(half3(tf, tf, tf), tex.rgb, alpha);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
