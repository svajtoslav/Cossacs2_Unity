Shader "Cossacks2Bridge/WaterLikeOriginalV2"
{
    Properties
    {
        _CloudTex ("Cloud / RefSky Texture", 2D) = "gray" {}
        _DeepColor ("Deep Color", Color) = (0.02,0.40,0.48,1)
        _ShallowColor ("Shallow Color", Color) = (0.34,0.68,0.62,1)
        _FoamColor ("Foam / Cloud White", Color) = (0.90,0.96,0.93,1)
        _CloudScale ("World Cloud Scale", Float) = 0.00042
        _CloudStrength ("Cloud Strength", Float) = 3.50
        _WaterOpacity ("Water Opacity", Float) = 0.95
        _WaveStrength ("Wave Strength", Float) = 0.74
        _CameraInfluence ("Camera Influence", Float) = 0.0
        _SkyReflectStrength ("RefSky Strength", Float) = 2.60
        _SkyDarkStrength ("RefSky Dark Strength", Float) = 0.36
        _RippleLineStrength ("Ripple Line Strength", Float) = 0.12
        _BumpDistortStrength ("Bump Distort Strength", Float) = 0.014
        _ScreenCloudScale ("Screen RefSky Scale", Float) = 0.0
        _ScreenCloudStrength ("Screen RefSky Strength", Float) = 0.0
        _BottomFadeStrength ("Bottom Fade Strength", Float) = 0.38
        _RefSkyOverlayStrength ("RefSky Overlay Strength", Float) = 0.78
        _RefSkyOverlayScale ("RefSky Overlay Scale", Float) = 0.62
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
            // V14 base pass: darker RefSky, shore alpha fade, soft random ripple patches.
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
            fixed4 _DeepColor;
            fixed4 _ShallowColor;
            fixed4 _FoamColor;
            float _CloudScale;
            float _CloudStrength;
            float _WaterOpacity;
            float _WaveStrength;
            float _CameraInfluence;
            float _SkyReflectStrength;
            float _SkyDarkStrength;
            float _RippleLineStrength;
            float _BumpDistortStrength;
            float _ScreenCloudScale;
            float _ScreenCloudStrength;
            float _BottomFadeStrength;
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
                float3 world : TEXCOORD0;
                float2 data : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.data = v.uv1;
                o.color = v.color;
                return o;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _C2Time;
                float2 w = i.world.xz;

                fixed deep01 = saturate(i.data.x);
                fixed shore01 = saturate(i.data.y);

                float rA = sin(dot(w, float2(0.170, 0.121)) + t * 1.12);
                float rB = cos(dot(w, float2(-0.143, 0.154)) - t * 1.30);
                float rC = sin(dot(w, float2(0.236, -0.092)) + t * 0.98);
                float rD = cos(dot(w, float2(0.101, 0.214)) - t * 1.08);
                float2 bump = float2(rA * 0.55 + rC * 0.24 - rD * 0.16,
                                     rB * 0.56 - rA * 0.14 + rD * 0.18)
                              * (_BumpDistortStrength * (0.40 + deep01 * 0.92));

                // Camera-independent world-space RefSky. _CloudOffset now runs at the original DT speed.
                float2 drift = _CloudOffset.xy;
                float2 skyUv0 = w * _CloudScale + drift + bump;
                float2 skyUv1 = w * (_CloudScale * 0.54) + drift * 0.72 - bump * 0.38 + float2(0.17, -0.09);
                float2 skyUv2 = w * (_CloudScale * 0.30) + drift * 0.39 + float2(-0.23, 0.14);
                float2 skyUv3 = w * (_CloudScale * 1.35) + drift * 1.12 + bump * 0.20 + float2(0.06, 0.21);

                fixed3 sky0 = sqrt(saturate(tex2D(_CloudTex, skyUv0).rgb));
                fixed3 sky1 = sqrt(saturate(tex2D(_CloudTex, skyUv1).rgb));
                fixed3 sky2 = sqrt(saturate(tex2D(_CloudTex, skyUv2).rgb));
                fixed3 sky3 = sqrt(saturate(tex2D(_CloudTex, skyUv3).rgb));
                fixed3 skyMix = lerp(sky0, sky1, 0.22);
                fixed skyLum = dot(lerp(skyMix, sky2, 0.12), fixed3(0.30, 0.59, 0.11));
                fixed skyFine = dot(sky3, fixed3(0.30, 0.59, 0.11));

                // IMPORTANT: oblaka123g1 luminance range is about 0.10..0.41.
                // These thresholds intentionally use that real range.
                fixed cloudSoft = smoothstep(0.125, 0.215, skyLum);
                fixed cloudCore = smoothstep(0.175, 0.285, skyLum);
                fixed cloudHot  = smoothstep(0.235, 0.350, skyLum);
                fixed darkCloud = 1.0 - smoothstep(0.120, 0.220, lerp(skyLum, skyFine, 0.20));

                fixed3 water = lerp(_ShallowColor.rgb, _DeepColor.rgb, deep01);
                water = lerp(water, i.color.rgb, 0.030);

                // Keep the good V6/V7 base, but do not let it swallow RefSky.
                fixed bottomFade = saturate(_BottomFadeStrength * (0.16 + deep01 * 0.72) * (1.0 - shore01 * 0.32));
                water = lerp(water, _DeepColor.rgb * 0.98, bottomFade * 0.28);

                fixed3 col = water;
                fixed deepMask = saturate(0.28 + deep01 * 0.68);

                // Dark body of clouds, like the reflected sky target in the original renderer.
                col = lerp(col, col * fixed3(0.74, 0.86, 0.88), darkCloud * deepMask * _SkyDarkStrength);

                // Visible white/sky cloud reflection. It is intentionally a separate layer,
                // because the old BumpWater shader sampled a RefSky render target here.
                fixed3 liftedCloud = saturate((skyMix - 0.105) * 5.8);
                fixed3 skyBlue = fixed3(0.38, 0.82, 0.82);
                fixed3 cloudColor = lerp(skyBlue, _FoamColor.rgb, saturate(cloudCore * 0.82 + cloudHot * 0.55));
                cloudColor = lerp(cloudColor, liftedCloud * fixed3(0.72, 0.96, 0.88) + _FoamColor.rgb * 0.42, 0.34);
                fixed cloudVeil = saturate(cloudSoft * _CloudStrength * _SkyReflectStrength * 0.035);
                fixed cloudMask = saturate((cloudCore * 0.58 + cloudHot * 0.62) * _CloudStrength * _SkyReflectStrength * 0.155 + cloudVeil * 0.18);

                col = lerp(col, cloudColor, saturate((cloudVeil * 0.24 + cloudMask) * deepMask));
                col += _FoamColor.rgb * saturate(cloudHot * deepMask * 0.18);

                float patchT = t * 0.105;
                float patchFrame = floor(patchT);
                float patchBlend = smoothstep(0.12, 0.88, frac(patchT));
                float patchA = ValueNoise(w * 0.022 + float2(patchFrame * 17.13, -patchFrame * 9.27));
                float patchB = ValueNoise(w * 0.022 + float2((patchFrame + 1.0) * 17.13, -(patchFrame + 1.0) * 9.27));
                float patchNoise = lerp(patchA, patchB, patchBlend);
                float localNoise = ValueNoise(w * 0.070 + float2(t * 0.043, -t * 0.031));
                float ripplePatch = smoothstep(0.56, 0.82, patchNoise) * (0.35 + localNoise * 0.65);

                float angleWarp = (ValueNoise(w * 0.035 + float2(t * 0.020, -t * 0.015)) - 0.5) * 0.055;
                float lineA = sin(w.y * 0.34 + w.x * (0.044 + angleWarp) + t * 2.20 + rA * 0.35);
                float lineB = sin(w.y * 0.19 - w.x * (0.030 - angleWarp * 0.55) - t * 1.62 + rB * 0.30);
                float lineC = sin(w.y * 0.49 + w.x * (0.017 + angleWarp * 0.35) + t * 2.68 + rC * 0.24);
                float breakup = saturate(ValueNoise(w * 0.115 + float2(t * 0.090, -t * 0.050)));
                float brightLines = (pow(saturate(lineA * 0.5 + 0.5), 15.0) * 0.34
                                   + pow(saturate(lineB * 0.5 + 0.5), 18.0) * 0.26
                                   + pow(saturate(lineC * 0.5 + 0.5), 22.0) * 0.18)
                                  * (0.10 + breakup * 0.42);
                float rippleShadow = saturate((-lineA * lineB) * 0.5 + 0.5);
                fixed ripplePresence = saturate(0.08 + ripplePatch * 0.92);
                fixed rippleMask = saturate((0.10 + deep01 * 0.82) * _RippleLineStrength * ripplePresence);
                col += _FoamColor.rgb * brightLines * rippleMask;
                col = lerp(col, col * 0.93, rippleShadow * rippleMask * 0.15);

                // Minimal shore brightening.
                col = lerp(col, _FoamColor.rgb, shore01 * (1.0 - deep01) * 0.010);

                fixed alphaFromMap = saturate(i.color.a * _WaterOpacity);
                fixed alphaShape = saturate((0.34 + deep01 * 0.39 + shore01 * 0.09) * _WaterOpacity);
                fixed alpha = max(alphaFromMap, alphaShape);
                alpha = saturate(alpha + (cloudVeil * 0.030 + cloudMask * 0.075 + cloudHot * 0.030) * deepMask);
                alpha *= lerp(0.68, 1.0, smoothstep(0.00, 0.20, deep01));

                clip(alpha - 0.008);
                return fixed4(saturate(col), alpha);
            }
            ENDCG
        }

        Pass
        {
            // V14 second pass: darker, softened gamma-corrected RefSky overlay.
            ZWrite Off
            ZTest LEqual
            Offset -2, -2
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _CloudTex;
            fixed4 _FoamColor;
            float4 _CloudOffset;
            float _C2Time;
            float _RefSkyOverlayStrength;
            float _RefSkyOverlayScale;
            float _BumpDistortStrength;

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
                float2 data : TEXCOORD1;
                float3 world : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.data = v.uv1;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed3 SampleCloudGamma(float2 uv)
            {
                return sqrt(saturate(tex2D(_CloudTex, uv).rgb));
            }

            fixed3 SampleCloudSoft(float2 uv)
            {
                fixed3 a = SampleCloudGamma(uv);
                fixed3 b = SampleCloudGamma(uv + float2(0.006, 0.003));
                fixed3 c = SampleCloudGamma(uv + float2(-0.004, 0.007));
                fixed3 d = SampleCloudGamma(uv + float2(0.008, -0.005));
                return (a + b + c + d) * 0.25;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed deep01 = saturate(i.data.x);
                fixed shore01 = saturate(i.data.y);
                fixed deepMask = saturate(0.18 + deep01 * 0.82);

                float t = _C2Time;
                float2 w = i.world.xz;
                float2 uv = i.uv * _RefSkyOverlayScale;
                float2 drift = _CloudOffset.xy;

                float waveA = sin(dot(w, float2(0.145, 0.082)) + t * 1.05);
                float waveB = cos(dot(w, float2(-0.091, 0.137)) - t * 1.22);
                float2 bump = float2(waveA, waveB) * (_BumpDistortStrength * (0.50 + deep01 * 0.90));

                float2 skyUv0 = uv + drift + bump;
                float2 skyUv1 = uv * 0.52 + drift * 0.66 - bump * 0.35 + float2(0.19, -0.11);
                float2 skyUv2 = uv * 0.82 + drift * 0.92 + bump * 0.12 + float2(-0.07, 0.23);

                fixed3 sky0 = SampleCloudSoft(skyUv0);
                fixed3 sky1 = SampleCloudSoft(skyUv1);
                fixed3 sky2 = SampleCloudSoft(skyUv2);
                fixed skyLum = dot(lerp(sky0, sky1, 0.30), fixed3(0.30, 0.59, 0.11));
                fixed fineLum = dot(sky2, fixed3(0.30, 0.59, 0.11));

                fixed broad = smoothstep(0.118, 0.205, skyLum);
                fixed core = smoothstep(0.158, 0.270, skyLum);
                fixed hot = smoothstep(0.215, 0.340, max(skyLum, fineLum * 0.92));

                fixed cloud = saturate(broad * 0.22 + core * 0.50 + hot * 0.36);
                fixed shoreDepthMask = smoothstep(0.02, 0.22, deep01);
                fixed alpha = saturate(cloud * deepMask * shoreDepthMask * _RefSkyOverlayStrength);
                alpha *= saturate(1.0 - shore01 * 0.32);

                fixed3 skyBlue = fixed3(0.32, 0.70, 0.72);
                fixed3 cloudWhite = fixed3(0.72, 0.92, 0.88);
                fixed3 col = lerp(skyBlue, cloudWhite, saturate(core * 0.78 + hot * 0.62));
                col = lerp(col, _FoamColor.rgb, hot * 0.12);

                clip(alpha - 0.010);
                return fixed4(saturate(col), alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
