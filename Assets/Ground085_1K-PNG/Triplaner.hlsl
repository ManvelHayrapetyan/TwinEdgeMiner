#ifndef TWIN_EDGE_MINER_TRIPLANAR_INCLUDED
#define TWIN_EDGE_MINER_TRIPLANAR_INCLUDED

void Triplanar_float(
    UnityTexture2D Texture,
    float3 PositionWS,
    float3 NormalWS,
    float Tiling,
    float Blend,
    out float3 Color)
{
    float3 normal = normalize(NormalWS);
    float safeTiling = max(abs(Tiling), 0.0001);
    float sharpness = max(Blend, 0.0001);

    float3 weights = pow(abs(normal), sharpness);
    weights /= max(weights.x + weights.y + weights.z, 0.0001);

    float3 scaledPosition = PositionWS * safeTiling;
    float3 scaledPositionDx = ddx(scaledPosition);
    float3 scaledPositionDy = ddy(scaledPosition);

    float signX = normal.x < 0.0 ? -1.0 : 1.0;
    float signY = normal.y < 0.0 ? -1.0 : 1.0;
    float signZ = normal.z < 0.0 ? -1.0 : 1.0;

    float2 uvX = scaledPosition.zy * float2(signX, 1.0);
    float2 uvY = scaledPosition.xz * float2(signY, 1.0);
    float2 uvZ = scaledPosition.xy * float2(-signZ, 1.0);

    float2 dxX = scaledPositionDx.zy * float2(signX, 1.0);
    float2 dxY = scaledPositionDx.xz * float2(signY, 1.0);
    float2 dxZ = scaledPositionDx.xy * float2(-signZ, 1.0);

    float2 dyX = scaledPositionDy.zy * float2(signX, 1.0);
    float2 dyY = scaledPositionDy.xz * float2(signY, 1.0);
    float2 dyZ = scaledPositionDy.xy * float2(-signZ, 1.0);

    float3 colorX = SAMPLE_TEXTURE2D_GRAD(Texture.tex, Texture.samplerstate, uvX, dxX, dyX).rgb;
    float3 colorY = SAMPLE_TEXTURE2D_GRAD(Texture.tex, Texture.samplerstate, uvY, dxY, dyY).rgb;
    float3 colorZ = SAMPLE_TEXTURE2D_GRAD(Texture.tex, Texture.samplerstate, uvZ, dxZ, dyZ).rgb;

    Color = colorX * weights.x + colorY * weights.y + colorZ * weights.z;
}

float3 TriplanarUnpackNormal(float4 packedNormal, float strength, float flipGreen)
{
    packedNormal.x *= packedNormal.w;

    float3 normalTS;
    normalTS.xy = packedNormal.xy * 2.0 - 1.0;
    normalTS.y = lerp(normalTS.y, -normalTS.y, saturate(flipGreen));
    normalTS.xy *= max(strength, 0.0);
    normalTS.z = sqrt(max(1.0 - saturate(dot(normalTS.xy, normalTS.xy)), 0.0001));
    return normalize(normalTS);
}

void TriplanarNormal_float(
    UnityTexture2D NormalTexture,
    float3 PositionWS,
    float3 NormalWS,
    float3 TangentWS,
    float Tiling,
    float Blend,
    float Strength,
    float FlipGreen,
    out float3 NormalTS)
{
    float3 surfaceNormalWS = normalize(NormalWS);
    float safeTiling = max(abs(Tiling), 0.0001);
    float sharpness = max(Blend, 0.0001);

    float3 weights = pow(abs(surfaceNormalWS), sharpness);
    weights /= max(weights.x + weights.y + weights.z, 0.0001);

    float3 scaledPosition = PositionWS * safeTiling;
    float3 scaledPositionDx = ddx(scaledPosition);
    float3 scaledPositionDy = ddy(scaledPosition);

    float signX = surfaceNormalWS.x < 0.0 ? -1.0 : 1.0;
    float signY = surfaceNormalWS.y < 0.0 ? -1.0 : 1.0;
    float signZ = surfaceNormalWS.z < 0.0 ? -1.0 : 1.0;

    float2 uvX = scaledPosition.zy * float2(signX, 1.0);
    float2 uvY = scaledPosition.xz * float2(signY, 1.0);
    float2 uvZ = scaledPosition.xy * float2(-signZ, 1.0);

    float2 dxX = scaledPositionDx.zy * float2(signX, 1.0);
    float2 dxY = scaledPositionDx.xz * float2(signY, 1.0);
    float2 dxZ = scaledPositionDx.xy * float2(-signZ, 1.0);

    float2 dyX = scaledPositionDy.zy * float2(signX, 1.0);
    float2 dyY = scaledPositionDy.xz * float2(signY, 1.0);
    float2 dyZ = scaledPositionDy.xy * float2(-signZ, 1.0);

    float3 tangentNormalX = TriplanarUnpackNormal(
        SAMPLE_TEXTURE2D_GRAD(NormalTexture.tex, NormalTexture.samplerstate, uvX, dxX, dyX),
        Strength,
        FlipGreen);
    float3 tangentNormalY = TriplanarUnpackNormal(
        SAMPLE_TEXTURE2D_GRAD(NormalTexture.tex, NormalTexture.samplerstate, uvY, dxY, dyY),
        Strength,
        FlipGreen);
    float3 tangentNormalZ = TriplanarUnpackNormal(
        SAMPLE_TEXTURE2D_GRAD(NormalTexture.tex, NormalTexture.samplerstate, uvZ, dxZ, dyZ),
        Strength,
        FlipGreen);

    float3 normalX = float3(tangentNormalX.z * signX, tangentNormalX.y, tangentNormalX.x * signX);
    float3 normalY = float3(tangentNormalY.x * signY, tangentNormalY.z * signY, tangentNormalY.y);
    float3 normalZ = float3(tangentNormalZ.x * -signZ, tangentNormalZ.y, tangentNormalZ.z * signZ);

    float3 normalWS = normalize(normalX * weights.x + normalY * weights.y + normalZ * weights.z);
    normalWS = dot(normalWS, surfaceNormalWS) < 0.0 ? -normalWS : normalWS;

    float3 tangentWS = normalize(TangentWS);
    float3 bitangentWS = normalize(cross(surfaceNormalWS, tangentWS));

    NormalTS = normalize(float3(
        dot(normalWS, tangentWS),
        dot(normalWS, bitangentWS),
        max(dot(normalWS, surfaceNormalWS), 0.0001)));
}

#endif
