Shader "Cossacks2Bridge/Road"
{
    Properties { _MainTex ("Road Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            v2f vert(appdata v){ v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; o.color=v.color; return o; }
            half4 frag(v2f i):SV_Target {
                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half3 rgb=saturate(i.color.rgb*tex.rgb*2.0h);
                half a=saturate(i.color.a*tex.a*2.0h);
                clip(a-(16.0h/255.0h));
                return half4(rgb,a);
            }
            ENDHLSL
        }
    }
}
