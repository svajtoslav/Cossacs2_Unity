Shader "Cossacks2Bridge/RoadLayerV16"
{
    Properties
    {
        _MainTex ("Road Texture", 2D) = "white" {}
        _BaseMap ("Road Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _RoadColorBoost ("Road Color Boost", Float) = 2.0
        _RoadAlphaBoost ("Road Alpha Boost", Float) = 2.0
        _RoadAlphaRef ("Road Alpha Ref", Float) = 0.0627451
        _UseTextureAlpha ("Use Texture Alpha", Float) = 1.0
        _RoadVFlip ("Road D3D V Flip", Float) = 0.0
        _RoadRgbAlphaFallback ("Road RGB Alpha Fallback", Float) = 0.0
        _RoadRgbAlphaBoost ("Road RGB Alpha Boost", Float) = 1.0
        _RoadClipDepthPull ("Road Clip Depth Pull", Float) = 0.0016
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+600"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            // V16: hardware ZTest remains enabled. The road mesh now samples the exact same
            // terrain triangle height, so it no longer gets camera-dependent surface cuts.
            Offset -4, -4
            Cull Off

            // Original road.xml color/alpha is kept from V8/V9.
            // Unity adaptation: depth test remains enabled so roads do not render through hills; late queue prevents terrain transparent layers from drawing over roads.
            // Original screen-space path had its own terrain draw ordering; in Unity world-space we need ZTest LEqual.
            // Original road.xml:
            // SrcBlend=One, DestBlend=InvSrcAlpha, AlphaTest >=16.
            // Stage0: Color=Modulate2x(Diffuse,Texture), Alpha=Modulate2x(Texture,Diffuse).
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _BaseColor;
            half _RoadColorBoost;
            half _RoadAlphaBoost;
            half _RoadAlphaRef;
            half _UseTextureAlpha;
            half _RoadVFlip;
            half _RoadRgbAlphaFallback;
            half _RoadRgbAlphaBoost;
            half _RoadClipDepthPull;

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

                // V16: keep only a tiny clip-depth epsilon for floating-point precision.
                // The actual no-sink fix is CPU-side exact terrain-triangle height sampling.
                #if defined(UNITY_REVERSED_Z)
                    o.pos.z += _RoadClipDepthPull * o.pos.w;
                #else
                    o.pos.z -= _RoadClipDepthPull * o.pos.w;
                #endif

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (_RoadVFlip > 0.5h)
                    uv.y = 1.0h - uv.y;

                fixed4 tex = tex2D(_MainTex, uv);

                half diffuseA = saturate(i.color.a * _Color.a * _BaseColor.a);
                half texA = lerp(1.0h, tex.a, saturate(_UseTextureAlpha));
                half a = saturate(texA * diffuseA * _RoadAlphaBoost);

                // Safety for wide roads: some old TGA road masks carry useful road body
                // in RGB after Unity/D3D origin conversion. This does not affect trails.
                half rgbMask = saturate(dot(tex.rgb, half3(0.3333h, 0.3333h, 0.3334h)) * _RoadRgbAlphaBoost);
                a = max(a, saturate(rgbMask * diffuseA * _RoadRgbAlphaFallback));

                clip(a - _RoadAlphaRef);

                half3 rgb = saturate(tex.rgb * i.color.rgb * _RoadColorBoost) * _Color.rgb * _BaseColor.rgb;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack Off
}
