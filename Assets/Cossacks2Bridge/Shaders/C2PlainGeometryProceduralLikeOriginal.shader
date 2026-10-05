Shader "Hidden/C2/PlainGeometryProceduralLikeOriginal"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.74, 0.74, 0.74, 1.0)
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            struct RuntimeSubmitVertexLikeOriginal
            {
                float3 Position;
                uint Color;
                float2 Uv0;
                float4 Uv2;
            };

            StructuredBuffer<RuntimeSubmitVertexLikeOriginal> _Vertices;
            StructuredBuffer<uint> _Indices;
            float4 _BaseColor;
            float4x4 _C2ObjectToWorld;
            float4x4 _C2MatrixVP;

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR0;
            };

            v2f vert(uint vertexID : SV_VertexID)
            {
                v2f o;
                uint srcIndex = _Indices[vertexID];
                RuntimeSubmitVertexLikeOriginal v = _Vertices[srcIndex];
                float4 worldPos = mul(_C2ObjectToWorld, float4(v.Position, 1.0));
                o.positionCS = mul(_C2MatrixVP, worldPos);
                o.color = _BaseColor;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}
