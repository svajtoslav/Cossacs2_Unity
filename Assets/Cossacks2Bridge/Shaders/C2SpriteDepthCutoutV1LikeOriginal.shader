Shader "Cossacks2Bridge/C2SpriteDepthCutoutV1LikeOriginal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _C2ScreenAffine ("C2 Building Screen Affine", Float) = 0
        _BaseMap ("Base Map", 2D) = "white" {}
        _C2CameraFacing ("Nature Camera Facing", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.0156863
        _AlphaCutoff ("Alpha Cutoff Alias", Range(0,1)) = 0.0156863
        _C2IgnoreDarkShadowPixels ("C2 Ignore Dark Shadow Pixels", Float) = 0
        _C2ShadowMaxAlpha ("C2 Shadow Max Alpha", Range(0,1)) = 0.985
        _C2ShadowMaxLuma ("C2 Shadow Max Luma", Range(0,1)) = 0.52
        _C2ShadowMaxSaturation ("C2 Shadow Max Saturation", Range(0,1)) = 0.32
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        _OffsetFactor ("Depth Offset Factor", Float) = 0
        _OffsetUnits ("Depth Offset Units", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="TransparentCutout"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ColorMask 0
            ZWrite On
            ZTest [_ZTest]
            Cull [_Cull]
            Offset [_OffsetFactor], [_OffsetUnits]
            Lighting Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "C2NatureBillboard.hlsl"

            sampler2D _MainTex;
            sampler2D _BaseMap;
            float4 _MainTex_ST;
            float _Cutoff;
            float _C2ScreenAffine;
            float _AlphaCutoff;
            float _C2CameraFacing;
            float _C2IgnoreDarkShadowPixels;
            float _C2ShadowMaxAlpha;
            float _C2ShadowMaxLuma;
            float _C2ShadowMaxSaturation;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 spriteOffset : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(C2NatureBillboardWorld(v.vertex.xyz, v.spriteOffset, _C2CameraFacing), 1));
                // Keep projected XYZ and LINESORT depth; interpolate screen-sprite UVs affinely.
                if (_C2ScreenAffine > 0.5 && o.pos.w > 0.0) o.pos /= o.pos.w;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                if (_C2IgnoreDarkShadowPixels > 0.5)
                {
                    float maxc = max(c.r, max(c.g, c.b));
                    float minc = min(c.r, min(c.g, c.b));
                    if (c.a <= _C2ShadowMaxAlpha &&
                        maxc <= _C2ShadowMaxLuma &&
                        (maxc - minc) <= _C2ShadowMaxSaturation)
                    {
                        discard;
                    }
                }
                clip(c.a - max(_Cutoff, _AlphaCutoff));
                return 0;
            }
            ENDCG
        }
    }

    Fallback Off
}
