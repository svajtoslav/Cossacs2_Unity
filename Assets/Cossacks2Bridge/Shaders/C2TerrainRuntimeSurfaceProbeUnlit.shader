Shader "Cossacks2Bridge/SurfaceProbeUnlit"
{
    Properties
    {
        _C2UseStrictTnL("Use strict XYZRHW-like path", Float) = 1
        _C2DirectClipSpace("Use direct clip-space submit", Float) = 0
        _C2ProbeColor("Probe Color", Color) = (1,1,1,1)
        _C2ProbeSrcBlend("Strict STriang probe src blend", Float) = 1
        _C2ProbeDstBlend("Strict STriang probe dst blend", Float) = 0
        _C2ProbeCull("Strict STriang probe cull", Float) = 0
        _C2ProbeZWrite("Strict STriang probe zwrite", Float) = 0
        _C2ProbeZTest("Strict STriang probe ztest", Float) = 8
        _C2ProbeTransformMode("Strict STriang probe transform mode", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull [_C2ProbeCull]
        ZWrite [_C2ProbeZWrite]
        ZTest [_C2ProbeZTest]

        Pass
        {
            Name "ProbeUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend [_C2ProbeSrcBlend] [_C2ProbeDstBlend]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _C2Viewport;
            float _C2UseStrictTnL;
            float _C2DirectClipSpace;
            float4 _C2ProbeColor;
            float _C2ProbeTransformMode;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float4 uv2        : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float SafeReciprocalLikeOriginal(float v)
            {
                return abs(v) > 1e-6 ? rcp(v) : 1.0;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float clipW = (_C2DirectClipSpace > 0.5) ? 1.0 : SafeReciprocalLikeOriginal(v.uv2.x);
                float ndcX = v.positionOS.x;
                float ndcY = v.positionOS.y;
                float ndcZ = v.positionOS.z;
                if (_C2DirectClipSpace <= 0.5)
                {
                    float safeW = max(_C2Viewport.x, 1.0);
                    float safeH = max(_C2Viewport.y, 1.0);
                    float pxX = v.positionOS.x;
                    float pxY = v.positionOS.y;
                    if (_C2ProbeTransformMode > 1.5 && _C2ProbeTransformMode < 2.5)
                    {
                        pxX += 0.5;
                        pxY += 0.5;
                    }
                    else if (_C2ProbeTransformMode > 2.5)
                    {
                        pxX += 0.5;
                        pxY += 0.5;
                    }

                    ndcX = ((pxX - _C2Viewport.z) / safeW) * 2.0 - 1.0;

                    bool bottomLeftLike = (_C2ProbeTransformMode > 0.5 && _C2ProbeTransformMode < 1.5) || (_C2ProbeTransformMode > 2.5);
                    if (bottomLeftLike)
                        ndcY = ((pxY - _C2Viewport.w) / safeH) * 2.0 - 1.0;
                    else
                        ndcY = 1.0 - ((pxY - _C2Viewport.w) / safeH) * 2.0;

                    ndcZ = v.positionOS.z;
                }
                o.positionHCS = float4(ndcX * clipW, ndcY * clipW, ndcZ * clipW, clipW);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(_C2ProbeColor.rgb, _C2ProbeColor.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
