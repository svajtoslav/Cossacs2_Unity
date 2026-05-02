Shader "Cossacks2Bridge/NatureTreeShadowV16LikeOriginal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Diffuse Shadow Color", Color) = (0.5,0.5,0.5,1)
        _AlphaCutoff ("Alpha Ref", Range(0,1)) = 0.0156863
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
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
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _AlphaCutoff;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed a = tex.a * _Color.a * i.color.a;
                clip(a - _AlphaCutoff);
                return fixed4(_Color.rgb * i.color.rgb, a);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
