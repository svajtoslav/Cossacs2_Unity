Shader "Cossacks2/C2FogOfWarOverlayLikeOriginal"
{
    Properties
    {
        _MainTex ("FogOfWar.tga", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent+900" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;

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
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(i.color.a - (4.0 / 255.0));
                fixed3 textureColor = tex2Dlod(_MainTex, float4(i.uv, 0.0, 0.0)).rgb;
                return fixed4(saturate(textureColor * i.color.rgb * 2.0), i.color.a);
            }
            ENDCG
        }
    }
}
