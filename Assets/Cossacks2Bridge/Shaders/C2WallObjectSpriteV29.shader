Shader "Cossacks2Bridge/WallObjectSpriteV29"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.015
        _RepairAlphaMask ("Repair Alpha Mask", Float) = 0
        _RepairColor ("Repair Color", Color) = (0.46,0.43,0.35,1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 0
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
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _AlphaCutoff;
            float _RepairAlphaMask;
            float4 _RepairColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                c *= i.color;
                clip(c.a - _AlphaCutoff);

                if (_RepairAlphaMask > 0.5)
                {
                    fixed shade = lerp(0.72, 1.0, saturate(c.a));
                    c.rgb = _RepairColor.rgb * shade;
                    c.a *= _RepairColor.a;
                }
                return c;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
