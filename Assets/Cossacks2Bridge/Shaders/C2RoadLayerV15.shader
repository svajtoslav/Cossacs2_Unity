Shader "Cossacks2Bridge/RoadLayerV15"
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
        _RoadUseSceneDepth ("Use Camera Depth Occlusion", Float) = 1.0
        _RoadSceneDepthTolerance ("Scene Depth Tolerance", Float) = 6.0
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
            // V15: do not let the hardware depth test cut roads on the same terrain surface.
            // Fragment shader samples camera depth and discards only when the road is truly
            // behind terrain by more than _RoadSceneDepthTolerance.
            ZTest Always
            Offset -4, -4
            Cull Off

            // Original road.xml color/alpha is kept from V8/V9.
            // Unity adaptation: tolerant camera-depth occlusion replaces hard ZTest LEqual.
            // This prevents close-camera road tearing while still rejecting real hill occlusion.
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
            half _RoadUseSceneDepth;
            half _RoadSceneDepthTolerance;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float4 screenPos : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float4 screenPos : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // V14: world-space yOffset cannot solve the remaining cuts because the road fails
                // against the already written terrain depth. Pull only the road depth slightly
                // toward the camera in clip space. ZTest remains LEqual, so real hills/mountains
                // can still occlude roads when their depth gap is not just the surface epsilon.
                #if defined(UNITY_REVERSED_Z)
                    o.pos.z += _RoadClipDepthPull * o.pos.w;
                #else
                    o.pos.z -= _RoadClipDepthPull * o.pos.w;
                #endif

                o.screenPos = ComputeScreenPos(o.pos);
                COMPUTE_EYEDEPTH(o.eyeDepth);

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

                if (_RoadUseSceneDepth > 0.5h)
                {
                    float rawSceneDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                    float sceneEyeDepth = LinearEyeDepth(rawSceneDepth);

                    // If the scene depth is valid and the road is significantly behind it,
                    // this is a real occluder. Small differences are treated as same-surface
                    // terrain contact and the road is allowed to draw on top.
                    if (sceneEyeDepth > 0.0001 && (i.eyeDepth - sceneEyeDepth) > _RoadSceneDepthTolerance)
                        discard;
                }

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
