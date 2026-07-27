using UnityEngine;

public class CrackVisualTuner : MonoBehaviour
{
    private const string DefaultHelp =
        "Attach this to the voxel manager/root object. During Play Mode it applies these values to child MeshRenderer materials every frame. _CrackTex, _VoxelSize and _VoxelGridSize are runtime chunk data and are intentionally not tunable here.";

    private static readonly int MainTextureId = Shader.PropertyToID("_MainTexture");
    private static readonly int NormalId = Shader.PropertyToID("_Normal");
    private static readonly int AmbientOcclusionId = Shader.PropertyToID("_AmbientOcclusion");
    private static readonly int RoughnessId = Shader.PropertyToID("_Roughness");
    private static readonly int CrackText2DId = Shader.PropertyToID("_CrackText2D");

    private static readonly int TileId = Shader.PropertyToID("_Tile");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    private static readonly int NormalStrengthId = Shader.PropertyToID("_NormalStrength");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothnes");

    private static readonly int CrackThresholdId = Shader.PropertyToID("_CrackThreshold");
    private static readonly int CrackSoftnessId = Shader.PropertyToID("_CrackSoftness");
    private static readonly int CrackContrastId = Shader.PropertyToID("_CrackContrast");
    private static readonly int CrackPowerId = Shader.PropertyToID("_CrackPower");
    private static readonly int CrackMaskBoostId = Shader.PropertyToID("_CrackMaskBoost");
    private static readonly int CrackDarknessId = Shader.PropertyToID("_CrackDarkness");

    [Header("How To Use")]
    [TextArea(3, 5)]
    [SerializeField] private string _help = DefaultHelp;

    [Header("State")]
    [Tooltip("Enable only while tuning crack visuals in Play Mode. Keep this off for normal gameplay.")]
    [SerializeField] private bool _tuningEnabled;

    [Header("Ground Textures")]
    [Tooltip("Main ground albedo texture used by the ground Triplanar node.")]
    [SerializeField] private Texture2D _mainTexture;

    [Tooltip("Ground normal map used by TriplanarNormal.")]
    [SerializeField] private Texture2D _normal;

    [Tooltip("Ground ambient occlusion texture.")]
    [SerializeField] private Texture2D _ambientOcclusion;

    [Tooltip("Ground roughness texture.")]
    [SerializeField] private Texture2D _roughness;

    [Header("Crack Texture")]
    [Tooltip("2D crack line texture. This is only the line pattern; the 3D damage mask still comes from runtime _CrackTex.")]
    [SerializeField] private Texture2D _crackText2D;

    [Header("Triplanar Projection")]
    [Tooltip("Texture scale for ground and crack triplanar projection. Higher values repeat textures more often.")]
    [SerializeField, Range(0.01f, 5f)] private float _tile = 0.4f;

    [Tooltip("Triplanar blend sharpness. Higher values make projection transitions sharper.")]
    [SerializeField, Range(0.01f, 10f)] private float _blend = 5f;

    [Header("Surface")]
    [Tooltip("Strength of the ground normal map.")]
    [SerializeField, Range(0f, 2f)] private float _normalStrength = 0.5f;

    [Tooltip("Surface smoothness value sent to the shader graph.")]
    [SerializeField, Range(0f, 1f)] private float _smoothness = 0.7f;

    [Header("3D Damage Mask")]
    [Tooltip("Damage value where cracks start appearing. Lower values reveal cracks earlier.")]
    [SerializeField, Range(0f, 1f)] private float _crackThreshold = 0.3f;

    [Tooltip("Softens and widens the 3D damage mask. Higher values help cracks stay visible on marching-cubes surfaces.")]
    [SerializeField, Range(0f, 1f)] private float _crackSoftness = 0.5f;

    [Tooltip("Contrast of the 3D damage mask after thresholding.")]
    [SerializeField, Range(0.01f, 5f)] private float _crackContrast = 0.5f;

    [Header("Crack Line Look")]
    [Tooltip("Power applied to the 2D crack line texture. Values below 1 make faint lines stronger; values above 1 make lines thinner.")]
    [SerializeField, Range(0.05f, 5f)] private float _crackPower = 0.5f;

    [Tooltip("Final multiplier for Mask * CrackLines before Saturate. Higher values make cracks more visible.")]
    [SerializeField, Range(0f, 10f)] private float _crackMaskBoost = 3f;

    [Tooltip("How dark the ground becomes inside crack lines. 1 keeps original color, 0 is fully black.")]
    [SerializeField, Range(0f, 1f)] private float _crackDarkness = 0.15f;

    private void Update()
    {
        if (!_tuningEnabled)
            return;

        Apply();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_help))
            _help = DefaultHelp;

        if (!Application.isPlaying)
            return;

        if (!_tuningEnabled)
            return;

        Apply();
    }

    private void Apply()
    {
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (meshRenderer == null)
                continue;

            foreach (Material material in meshRenderer.materials)
            {
                if (material == null)
                    continue;

                ApplyToMaterial(material);
            }
        }
    }

    private void ApplyToMaterial(Material material)
    {
        SetTextureIfAssigned(material, MainTextureId, _mainTexture);
        SetTextureIfAssigned(material, NormalId, _normal);
        SetTextureIfAssigned(material, AmbientOcclusionId, _ambientOcclusion);
        SetTextureIfAssigned(material, RoughnessId, _roughness);
        SetTextureIfAssigned(material, CrackText2DId, _crackText2D);

        SetFloatIfPresent(material, TileId, _tile);
        SetFloatIfPresent(material, BlendId, _blend);
        SetFloatIfPresent(material, NormalStrengthId, _normalStrength);
        SetFloatIfPresent(material, SmoothnessId, _smoothness);

        SetFloatIfPresent(material, CrackThresholdId, _crackThreshold);
        SetFloatIfPresent(material, CrackSoftnessId, _crackSoftness);
        SetFloatIfPresent(material, CrackContrastId, _crackContrast);
        SetFloatIfPresent(material, CrackPowerId, _crackPower);
        SetFloatIfPresent(material, CrackMaskBoostId, _crackMaskBoost);
        SetFloatIfPresent(material, CrackDarknessId, _crackDarkness);
    }

    private static void SetTextureIfAssigned(Material material, int propertyId, Texture texture)
    {
        if (texture != null && material.HasProperty(propertyId))
            material.SetTexture(propertyId, texture);
    }

    private static void SetFloatIfPresent(Material material, int propertyId, float value)
    {
        if (material.HasProperty(propertyId))
            material.SetFloat(propertyId, value);
    }
}
