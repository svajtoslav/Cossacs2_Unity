Shader "Cossacks2Bridge/RoadBump2"
{
    Properties { _MainTex ("Bump Texture", 2D) = "gray" {} _BumpY1("BumpY1", Vector) = (0,0,0,0) _BumpY2("BumpY2", Vector) = (0,0,0,0) }
    SubShader
    {
        Tags { "Queue"="Transparent+3" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend DstColor SrcColor
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 uv2:TEXCOORD1; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 uv2:TEXCOORD1; float4 color:COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _BumpY1; float4 _BumpY2;
            v2f vert(appdata v){ v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; o.uv2=v.uv2; o.color=v.color; return o; }
            half4 frag(v2f i):SV_Target {
                half4 tex0=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half4 tex1=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv2);
                half a=saturate(i.color.a);
                half3 rgb=lerp(half3(0.5h,0.5h,0.5h), tex1.rgb, a);
                half alpha=lerp(0.5h, tex0.a, a);
                clip(alpha-(16.0h/255.0h));
                return half4(rgb,alpha);
            }
            ENDHLSL
        }
    }
}
