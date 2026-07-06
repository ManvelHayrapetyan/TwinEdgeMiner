void BuildVoxelCrackBlend_float(
    UnityTexture3D CrackTex,

    float3 ObjectPosition,
    float VoxelSize,
    float3 BoundsMax,

    float4 GroundColor,
    float4 CrackPatternColor,
    float4 DarkCrackColor,

    float Threshold,
    float Softness,
    float Contrast,

    out float4 Result,
    out float FinalCrackMask)
{
    float3 voxelUV = (ObjectPosition / VoxelSize + 0.5) / BoundsMax;
    voxelUV = saturate(voxelUV);

    float4 crackSample = SAMPLE_TEXTURE3D(
        CrackTex.tex,
        CrackTex.samplerstate,
        voxelUV
    );

    float crackAmount = saturate(crackSample.r);

    float brightness = dot(CrackPatternColor.a, float3(0.299, 0.587, 0.114));
    float patternMask = saturate(1.0 - brightness);

    float raw = crackAmount * patternMask;
    raw = pow(raw, max(Contrast, 0.0001));

    FinalCrackMask = smoothstep(
        Threshold,
        Threshold + max(Softness, 0.0001),
        raw
    );

    FinalCrackMask = saturate(FinalCrackMask);

    Result = lerp(GroundColor, DarkCrackColor, FinalCrackMask);
}