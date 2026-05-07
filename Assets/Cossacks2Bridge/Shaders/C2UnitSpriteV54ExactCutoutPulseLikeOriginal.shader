Shader "Cossacks2Bridge/C2UnitSpriteV55_BrightPulse"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Main Color (Tint)", Color) = (1,1,1,1)
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.05
        
        [Header(Selection Pulse Settings)]
        [Toggle] _SelectedPulse ("Is Selected?", Float) = 0
        _BaseBrightness ("Base Brightness", Range(1.0, 2.0)) = 1.0
        _PulseIntensity ("Pulse Extra Brightness", Range(0.0, 2.0)) = 1.5
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        
        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4 // LessEqual
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0 // Off
        [Toggle] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+670"
            "RenderType"="TransparentCutout"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            // Смещение для предотвращения Z-fighting на плоских поверхностях
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _AlphaCutoff;
            
            half _SelectedPulse;
            half _BaseBrightness;
            half _PulseIntensity;
            float _PulseSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv);
                
                // Жесткая отсечка прозрачности для стиля "Казаков"
                clip(texel.a - _AlphaCutoff);

                // Базовая яркость (обычно 1.0)
                half finalDiffuse = _BaseBrightness;

                // Если юнит выбран, добавляем яркость по синусоиде (только в плюс)
                if (_SelectedPulse > 0.5)
                {
                    // Создаем волну 0..1
                    half wave = (1.0 - cos(_Time.y * _PulseSpeed)) * 0.5;
                    // Добавляем к базовой яркости интенсивность, умноженную на волну
                    finalDiffuse += _PulseIntensity * wave;
                }

                fixed4 outc;
                // Умножаем текстуру на цвет (для мерцания кодом) и на вычисленную яркость
                outc.rgb = texel.rgb * _Color.rgb * finalDiffuse;
                outc.a = texel.a * _Color.a;
                
                return outc;
            }
            ENDCG
        }
    }
    Fallback "Transparent/Cutout/Diffuse"
}