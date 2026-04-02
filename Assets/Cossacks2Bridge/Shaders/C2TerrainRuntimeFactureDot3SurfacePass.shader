Shader "Cossacks2Bridge/TerrainRuntimeFactureDot3SurfacePass"
{
    Properties
    {
        _MainTex("Facture Tex", 2D) = "white" {}
        _NormalTex("Normal Tex", 2D) = "bump" {}
        // Kept only for material/property compatibility with existing runtime setup.
        _BumpContrast("Bump Contrast", Float) = 0.6
        _BumpBrightness("Bump Brightness", Float) = 1.0
        _C2UseStrictTnL("Use strict XYZRHW-like path", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        // D3D9 CullMode=CCW maps to Unity Cull Back here because the strict terrain host submits
        // clockwise front faces for the pretransformed surface path.
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "FactureDot3SurfacePass"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend DstColor SrcColor, One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            float4 _C2Viewport;
            float _C2UseStrictTnL;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv0        : TEXCOORD0;
                float4 uv2        : TEXCOORD2;
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

            // D3DTOP_DOTPRODUCT3 semantics:
            // result = saturate( dot( (arg1 * 2 - 1), (arg2 * 2 - 1) ) )
            // replicated to RGB.
            half DotProduct3LikeFixedFunction(half3 arg1, half3 arg2)
            {
                half3 s1 = arg1 * 2.0h - 1.0h;
                half3 s2 = arg2 * 2.0h - 1.0h;
                return saturate(dot(s1, s2));
            }

            half4 frag(Varyings i) : SV_Target
            {
                const half tf = (128.0h / 255.0h);

                // Stage 0:
                // Original dot3.xml uses Linear/Linear/None + Wrap.
                // ColorOp  = DotProduct3(Texture, Diffuse)
                // AlphaOp  = SelectArg1(Diffuse)
                half3 stage0Texture = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, frac(i.uv0)).rgb;
                half4 diffuse = i.diffuse;
                half stage0 = DotProduct3LikeFixedFunction(stage0Texture, diffuse.rgb);

                // Stage 1:
                // Original dot3.xml uses Linear/Linear/None + Wrap.
                // ColorOp  = Modulate(Texture, Current)
                half3 stage1Texture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frac(i.uv0)).rgb;
                half3 current = stage1Texture * stage0;

                // Stage 2:
                // No texture fetch; stage2 only blends Current with TFactor by Diffuse alpha.
                // ColorOp  = BlendDiffuseAlpha(Current, TFactor)
                // AlphaOp  = SelectArg1(Diffuse)
                half alpha = diffuse.a;
                half3 rgb = lerp(half3(tf, tf, tf), current, alpha);

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
