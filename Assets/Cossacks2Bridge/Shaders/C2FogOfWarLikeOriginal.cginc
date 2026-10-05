#ifndef C2_FOG_OF_WAR_LIKE_ORIGINAL_INCLUDED
#define C2_FOG_OF_WAR_LIKE_ORIGINAL_INCLUDED

sampler2D _C2FogMapLikeOriginal;
float _C2FogEnabledLikeOriginal;
float4 _C2FogWorldMapLikeOriginal;
float4 _C2FogWorldMap2LikeOriginal;
float4 _C2FogTextureInfoLikeOriginal;
float4 _C2FogColorLikeOriginal;

inline float C2FogAlphaLikeOriginal(float3 worldPosition)
{
    if (_C2FogEnabledLikeOriginal < 0.5)
        return 0.0;

    float gx = (worldPosition.x + _C2FogWorldMapLikeOriginal.x) * _C2FogWorldMapLikeOriginal.z;
    float worldZSign = _C2FogWorldMap2LikeOriginal.x;
    float rawZ = worldPosition.z / worldZSign + _C2FogWorldMapLikeOriginal.y;
    // Constant map-origin offset, matching C2OriginalWorldCoordinatesV371.
    float gy = (rawZ - _C2FogWorldMap2LikeOriginal.y) * _C2FogWorldMapLikeOriginal.w;

    float originalX = gx * 32.0;
    float originalY = gy * 32.0;
    float originalHeight = worldPosition.y * _C2FogWorldMap2LikeOriginal.z;
    float fogX = originalX / 128.0 + 3.0;
    float fogY = (originalY * 0.5 - originalHeight) / 64.0 + 3.0;
    float2 fogUv = (float2(fogX, fogY) + 0.5) * _C2FogTextureInfoLikeOriginal.y;
    float fogValue = tex2D(_C2FogMapLikeOriginal, saturate(fogUv)).r * 65535.0;

    // fog.cpp::GetF + Fog2B, literally: fmin=1500, shf=300,
    // GetF=5..158 and the 3D fog path doubles that value into alpha.
    float delta = clamp(1500.0 - fogValue, 0.0, 300.0);
    float getF = fogValue >= 1500.0 ? 5.0 : 5.0 + delta * (153.0 / 300.0);
    return saturate((getF * 2.0) / 255.0);
}

inline float3 C2ApplyFogOfWarLikeOriginal(float3 rgb, float3 worldPosition)
{
    // The active Cossacks II path returns directly to DrawFogInWorldSpace.
    // It uses one 40x40 overlay patch and does not darken world materials.
    return rgb;
}

#endif
