// TEXCOORD1 carries the local sprite offset already present in POSITION.
// Restore its anchor, then orient that offset to the drawing camera, just as
// ZBuffer.cpp::ShowZElement/GetRolledBillboardTransform. Both color and depth
// must use this identical transform. Zero keeps unit/building geometry intact.
float3 C2NatureBillboardWorld(float3 positionOS, float2 spriteOffset, float cameraFacing)
{
    float3 world = mul(unity_ObjectToWorld, float4(positionOS, 1)).xyz;
    if (cameraFacing > 0.5)
    {
        float3 offset = float3(spriteOffset, 0);
        world -= mul((float3x3)unity_ObjectToWorld, offset);
        world += mul((float3x3)UNITY_MATRIX_I_V, offset);
    }
    return world;
}
