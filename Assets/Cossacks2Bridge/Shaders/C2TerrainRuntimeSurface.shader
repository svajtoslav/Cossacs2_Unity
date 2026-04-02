Shader "Cossacks2Bridge/Surface"
{
    Properties
    {
        _MainTex("Texture0 GroundTex", 2D) = "white" {}
        _CrossTex("Texture1 BoundNew128", 2D) = "white" {}
        _C2UseStrictTnL("Use strict XYZRHW-like path", Float) = 1
        _C2DirectClipSpace("Use direct clip-space submit", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Surface"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CrossTex);
            SAMPLER(sampler_CrossTex);

            float4 _C2Viewport;
            float _C2UseStrictTnL;
            float _C2DirectClipSpace;

            struct Attributes
            {
                float4 positionOS : POSITION; // free path: world-like mesh; strict path: pretransformed screen x/y + ndc z
                float4 color      : COLOR;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float4 uv2        : TEXCOORD2; // x = rhw
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 diffuse      : COLOR;
                float2 uv0         : TEXCOORD0;
                float2 uv1         : TEXCOORD1;
                float  rhw         : TEXCOORD2;
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
        ndcX = ((v.positionOS.x - _C2Viewport.z) / max(_C2Viewport.x, 1.0)) * 2.0 - 1.0;
        ndcY = 1.0 - ((v.positionOS.y - _C2Viewport.w) / max(_C2Viewport.y, 1.0)) * 2.0;
        ndcZ = v.positionOS.z;
    }

    o.positionHCS = float4(ndcX * clipW, ndcY * clipW, ndcZ * clipW, clipW);
    o.rhw = (_C2DirectClipSpace > 0.5) ? 1.0 : v.uv2.x;
    o.diffuse = v.color;
    o.uv0 = v.uv0;
    o.uv1 = v.uv1;
    return o;
}

            half4 frag(Varyings i) : SV_Target
{
    // Surface.xml Stage 0:
    // ColorOp   = Modulate2x(Diffuse, Texture0)
    // AlphaOp   = SelectArg2(Diffuse)
    half4 tex0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv0);
    half3 stage0Rgb = saturate((half3)i.diffuse.rgb * tex0.rgb * 2.0h);
    half  stage0A   = saturate(i.diffuse.a);

    // Surface.xml Stage 1:
    // ColorOp   = SelectArg2(Current)
    // AlphaOp   = AddSigned(Texture1, Diffuse) = Arg1 + Arg2 - 0.5
    half4 tex1 = SAMPLE_TEXTURE2D(_CrossTex, sampler_CrossTex, i.uv1);
    half3 finalRgb = stage0Rgb;
    half  finalA   = saturate(tex1.a + i.diffuse.a - 0.5h);

    // Surface.xml RenderState:
    // AlphaTestEnable = True
    // AlphaFunc       = GreaterEqual
    // AlphaRef        = 39
    clip(finalA - (39.0h / 255.0h));

    return half4(finalRgb, finalA);
}
            ENDHLSL
        }
    }
    FallBack Off
}
