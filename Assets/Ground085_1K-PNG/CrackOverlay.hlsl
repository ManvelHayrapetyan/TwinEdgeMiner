#ifndef TWIN_EDGE_MINER_CRACK_OVERLAY_INCLUDED
#define TWIN_EDGE_MINER_CRACK_OVERLAY_INCLUDED

float TemSafeScalar(float value)
{
    return max(abs(value), 0.00001);
}

float3 TemSafeVector(float3 value)
{
    return max(abs(value), 0.00001);
}

float3 TemBuildCrackTextureUv(float3 positionOS, float voxelSize, float3 voxelGridSize)
{
    float3 voxelPosition = positionOS / TemSafeScalar(voxelSize);
    return saturate((voxelPosition + 0.5) / TemSafeVector(voxelGridSize));
}

float TemApplySoftThreshold(float value, float threshold, float softness)
{
    float halfSoftness = TemSafeScalar(softness) * 0.5;
    return smoothstep(threshold - halfSoftness, threshold + halfSoftness, value);
}

float TemSampleCrackBlend(UnityTexture3D crackTex, float3 crackTextureUv, float threshold, float softness, float contrast)
{
    float crackValue = SAMPLE_TEXTURE3D(crackTex.tex, crackTex.samplerstate, crackTextureUv).r;
    float thresholdedCrack = TemApplySoftThreshold(crackValue, threshold, softness);
    return pow(saturate(thresholdedCrack), max(contrast, 0.00001));
}

void CrackMaskOnly_float(
    UnityTexture3D CrackTex,
    float3 PositionOS,
    float VoxelSize,
    float3 VoxelGridSize,
    float CrackThreshold,
    float CrackSoftness,
    float CrackContrast,
    out float Mask)
{
    float3 crackTextureUv = TemBuildCrackTextureUv(PositionOS, VoxelSize, VoxelGridSize);
    Mask = TemSampleCrackBlend(CrackTex, crackTextureUv, CrackThreshold, CrackSoftness, CrackContrast);
}

#endif