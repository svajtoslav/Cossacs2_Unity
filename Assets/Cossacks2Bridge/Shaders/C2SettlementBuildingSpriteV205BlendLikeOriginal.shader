Shader "Cossacks2Bridge/SettlementBuildingSpriteV205BlendLikeOriginal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _C2ScreenAffine ("C2 Building Screen Affine", Float) = 0
        _BaseMap ("Base Map", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.0156863
        _AlphaCutoff ("Alpha Cutoff Alias", Range(0,1)) = 0.0156863
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 8
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            Lighting Off
            Fog { Mode Off }
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BaseMap;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Cutoff;
            float _C2ScreenAffine;
            float _AlphaCutoff;

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
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Keep projected XYZ and LINESORT depth; interpolate screen-sprite UVs affinely.
                if (_C2ScreenAffine > 0.5 && o.pos.w > 0.0) o.pos /= o.pos.w;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float cutoff = max(_Cutoff, _AlphaCutoff);
                clip(c.a - cutoff);
                return c;
            }
            ENDCG
        }
    }

    Fallback Off
}
