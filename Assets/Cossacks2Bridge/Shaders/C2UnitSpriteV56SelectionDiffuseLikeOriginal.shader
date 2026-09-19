Shader "Cossacks2Bridge/C2UnitSpriteV56SelectionDiffuseLikeOriginal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseMap ("Base Map", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _C2Diffuse ("C2 Diffuse", Color) = (1,1,1,1)
        _C2CameraFacing ("Nature Camera Facing", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.00392157
        _C2ShadowMaxAlpha ("C2 Shadow Max Alpha", Range(0,1)) = 0.985
        _C2ShadowMaxLuma ("C2 Shadow Max Luma", Range(0,1)) = 0.52
        _C2ShadowMaxSaturation ("C2 Shadow Max Saturation", Range(0,1)) = 0.32
        _C2ShadowAlphaMul ("C2 Shadow Alpha Mul", Range(0,4)) = 1.15
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 8
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "C2UnitSpriteV56SelectionDiffuseLikeOriginal"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "C2NatureBillboard.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _BaseColor;
                half4 _C2Diffuse;
                float _C2CameraFacing;
                half _Cutoff;
                half _C2ShadowMaxAlpha;
                half _C2ShadowMaxLuma;
                half _C2ShadowMaxSaturation;
                half _C2ShadowAlphaMul;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 spriteOffset : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformWorldToHClip(C2NatureBillboardWorld(input.positionOS.xyz, input.spriteOffset, _C2CameraFacing));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * _BaseColor * _C2Diffuse;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 c = texel * input.color;
                clip(texel.a * input.color.a - _Cutoff);

                half maxc = max(texel.r, max(texel.g, texel.b));
                half minc = min(texel.r, min(texel.g, texel.b));
                bool shadowPixel =
                    texel.a <= _C2ShadowMaxAlpha &&
                    maxc <= _C2ShadowMaxLuma &&
                    (maxc - minc) <= _C2ShadowMaxSaturation;

                c.a = shadowPixel ? saturate(c.a * _C2ShadowAlphaMul) : input.color.a;
                return c;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull [_Cull]
        Lighting Off
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "C2NatureBillboard.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _BaseColor;
            fixed4 _C2Diffuse;
            float _C2CameraFacing;
            float _Cutoff;
            float _C2ShadowMaxAlpha;
            float _C2ShadowMaxLuma;
            float _C2ShadowMaxSaturation;
            float _C2ShadowAlphaMul;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 spriteOffset : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(C2NatureBillboardWorld(v.vertex.xyz, v.spriteOffset, _C2CameraFacing), 1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _BaseColor * _C2Diffuse;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv);
                fixed4 c = texel * i.color;
                clip(texel.a * i.color.a - _Cutoff);

                float maxc = max(texel.r, max(texel.g, texel.b));
                float minc = min(texel.r, min(texel.g, texel.b));
                bool shadowPixel =
                    texel.a <= _C2ShadowMaxAlpha &&
                    maxc <= _C2ShadowMaxLuma &&
                    (maxc - minc) <= _C2ShadowMaxSaturation;

                c.a = shadowPixel ? saturate(c.a * _C2ShadowAlphaMul) : i.color.a;
                return c;
            }
            ENDCG
        }
    }

    FallBack Off
}
