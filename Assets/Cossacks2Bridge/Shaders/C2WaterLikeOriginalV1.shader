Shader "Cossacks2Bridge/WaterLikeOriginalV1"
{
    Properties
    {
        _CloudTex ("Cloud Reflection", 2D) = "gray" {}
        _DeepColor ("Deep Color", Color) = (0.00,0.48,0.56,1)
        _ShallowColor ("Shallow Color", Color) = (0.36,0.76,0.66,1)
        _FoamColor ("Foam Color", Color) = (0.86,0.94,0.88,1)
        _CloudScale ("Cloud Scale", Float) = 0.00070
        _CloudStrength ("Cloud Strength", Float) = 0.44
        _WaterOpacity ("Water Opacity", Float) = 1.0
        _WaveStrength ("Wave Strength", Float) = 0.74
        _CameraInfluence ("Camera Influence", Float) = 0.003
        _CloudOffset ("Cloud Offset", Vector) = (0,0,0,0)
        _C2Time ("Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+440"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _CloudTex;
            float4 _CloudTex_ST;
            fixed4 _DeepColor;
            fixed4 _ShallowColor;
            fixed4 _FoamColor;
            float _CloudScale;
            float _CloudStrength;
            float _WaterOpacity;
            float _WaveStrength;
            float _CameraInfluence;
            float4 _CloudOffset;
            float _C2Time;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 world : TEXCOORD1;
                float2 data : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _CloudTex);
                o.color = v.color;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.data = v.uv1;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 w = i.world.xz;
                float t = _C2Time;

                float waveA = sin(dot(w, float2(0.032, 0.020)) + t * 0.90);
                float waveB = cos(dot(w, float2(-0.022, 0.039)) + t * 0.72);
                float waveC = sin(dot(w, float2(0.070, -0.028)) - t * 1.35);
                float fine = sin(dot(w, float2(0.135, 0.055)) + t * 2.05);
                float2 ripple = float2(waveA + waveC * 0.30, waveB - fine * 0.18) * (_WaveStrength * 0.0045);

                float2 cameraSoft = _CloudOffset.zw * _CameraInfluence;
                float2 cloudUv = w * _CloudScale + _CloudOffset.xy + cameraSoft + ripple;
                fixed3 cloud0 = tex2D(_CloudTex, cloudUv).rgb;
                fixed3 cloud1 = tex2D(_CloudTex, cloudUv * 0.52 + float2(0.173, -0.097)).rgb;
                fixed3 cloud2 = tex2D(_CloudTex, cloudUv * 1.74 + float2(0.031, 0.019)).rgb;
                fixed cloudLum = dot(cloud0, fixed3(0.30, 0.59, 0.11));
                fixed cloudBroad = dot(cloud1, fixed3(0.30, 0.59, 0.11));
                fixed cloudFine = dot(cloud2, fixed3(0.30, 0.59, 0.11));
                fixed cloudShape = smoothstep(0.38, 0.78, lerp(cloudLum, cloudBroad, 0.60));
                fixed darkShape = smoothstep(0.58, 0.18, lerp(cloudLum, cloudFine, 0.45));
                fixed3 clouds = lerp(fixed3(0.00, 0.34, 0.39), fixed3(0.78, 0.94, 0.86), cloudShape);
                clouds = lerp(clouds, fixed3(0.00, 0.23, 0.27), darkShape * 0.36);

                fixed deep01 = saturate(i.data.x);
                fixed shore01 = saturate(i.data.y);
                fixed alpha = saturate(i.color.a * _WaterOpacity);

                fixed3 water = saturate(lerp(lerp(_ShallowColor.rgb, _DeepColor.rgb, deep01), i.color.rgb, 0.34));
                fixed3 softClouds = lerp(water, clouds, 0.70);
                fixed cloudMask = saturate(_CloudStrength * (0.16 + deep01 * 0.48));
                fixed3 clouded = lerp(water, softClouds, cloudMask);
                fixed rippleLines = sin(w.y * 0.185 + w.x * 0.028 + t * 1.55) * sin(w.y * 0.082 - w.x * 0.020 - t * 0.95);
                fixed shimmer = saturate((waveA * waveB + waveC * 0.30 + fine * 0.18 + rippleLines * 0.42) * 0.5 + 0.5);
                clouded += (shimmer - 0.5) * (0.062 + deep01 * 0.052);
                clouded = lerp(clouded, _FoamColor.rgb, shore01 * (1.0 - deep01) * 0.040);

                clip(alpha - 0.015);
                return fixed4(saturate(clouded), alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
