Shader "Cossacks2Bridge/OriginalModelV437"
{
    Properties
    {
        _MainTex ("Native stage 0", 2D) = "white" {}
        _EnvTex ("Native stage 1", 2D) = "white" {}
        _Nation ("Texture factor", Color) = (1,1,1,1)
        _Ambient ("GameLight ambient", Color) = (0,0,0,1)
        _Diffuse ("GameLight diffuse", Color) = (1,1,1,1)
        _LightDirection ("Direction toward GameLight", Vector) = (0,1,0,0)
        _Modulate ("Stage 0 multiplier", Float) = 1
        _Environment ("Camera reflection stage", Float) = 0
        _Specular ("GameMaterial specular", Float) = 0
        _AlphaCutoff ("Native AlphaRef", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry+450" "RenderType"="TransparentCutout" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _EnvTex;
            float4 _Nation, _Ambient, _Diffuse, _LightDirection;
            float _Modulate, _Environment, _Specular, _AlphaCutoff;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 reflection:TEXCOORD1; float3 diffuse:COLOR0; float3 specular:COLOR1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                float3 n=normalize(UnityObjectToWorldNormal(v.normal));
                float3 l=normalize(_LightDirection.xyz);
                float3 view=normalize(UNITY_MATRIX_V[2].xyz);
                // D3D fixed-function Gouraud lighting: GameMaterial is white,
                // specular power 70 (sgRoot.cpp), LOCALVIEWER defaults to false.
                float ndotl=dot(n,l);
                o.diffuse=saturate(_Ambient.rgb+_Diffuse.rgb*max(0,ndotl));
                o.specular=_Specular*(ndotl>0?pow(max(0,dot(n,normalize(l+view))),70):0);
                float3 reflected=mul((float3x3)UNITY_MATRIX_V,reflect(-view,n));
                // CameraSpaceReflectionVector uses XY without a sphere-map
                // remap. D3D texture V is inverted by the image upload.
                o.reflection=float2(reflected.x,1-reflected.y);
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float4 tex=tex2D(_MainTex,i.uv);
                float3 color=saturate(tex.rgb*i.diffuse*_Modulate);
                float alpha;
                if(_Environment>0.5)
                {
                    float4 env=tex2D(_EnvTex,i.reflection);
                    color*=env.rgb;alpha=env.a;
                }
                else
                {
                    // Native stage 1 BlendCurrentAlpha and Add alpha.
                    color=lerp(_Nation.rgb,color,tex.a);alpha=saturate(tex.a*2);
                }
                clip(alpha-_AlphaCutoff);
                return float4(saturate(color+i.specular),alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
