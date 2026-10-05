Shader "Cossacks2Bridge/WallC2MDambaTexturedV33"
{
    Properties
    {
        _MainTex ("DAMBA WALLS.g16 Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.015
        _AtlasRect ("DrawWChunk Atlas Rect xywh", Vector) = (0,0,1,1)
        _UseAtlasRect ("Use DrawWChunk Atlas Rect", Float) = 0
        _UseVertexColor ("Use Vertex Color", Float) = 0
        _LocalUvRect ("Chunk UV normalize minU minV invRangeU invRangeV", Vector) = (0,0,1,1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Geometry+450" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }

        Pass
        {
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            Offset -1, -1
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _AlphaCutoff;
            float4 _AtlasRect;
            fixed _UseAtlasRect;
            fixed _UseVertexColor;
            float4 _LocalUvRect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localUv : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.localUv = v.uv;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = lerp(fixed4(1,1,1,1), v.color, saturate(_UseVertexColor));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // V50 / TemnyLess parity:
                // DrawWChunk does NOT sample the whole G16 frame with raw mesh UV.
                // It clamps local chunk UV to 0..1 and remaps it into the exact square rect.
                float2 uv = i.uv;
                if (_UseAtlasRect > 0.5)
                {
                    // V51: old GPCO UVs are often stored in a per-chunk range,
                    // not in direct 0..1 local space. Normalize first, then clamp.
                    float2 luv = saturate((i.localUv - _LocalUvRect.xy) * _LocalUvRect.zw);
                    uv = _AtlasRect.xy + luv * _AtlasRect.zw;
                }

                fixed4 texel = tex2D(_MainTex, uv);
                clip(texel.a - _AlphaCutoff);

                fixed4 outc;
                outc.rgb = texel.rgb * _Color.rgb * i.color.rgb;
                outc.a = 1.0;
                return outc;
            }
            ENDCG
        }
    }

    FallBack Off
}
