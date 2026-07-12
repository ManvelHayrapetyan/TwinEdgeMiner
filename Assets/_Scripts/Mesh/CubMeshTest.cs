using UnityEngine;

public class CubMeshTest : MonoBehaviour
{
    private Renderer _renderer;

    private void Start()
    {
        // Debug texture for validating shader sampling of a 3D volume.
        Texture3D testTexture = new(16, 16, 16, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                for (int z = 0; z < 16; z++)
                    testTexture.SetPixel(x, y, z, new Color(0, y / 15f, z / 15f, 1f));

        testTexture.Apply();

        _renderer = GetComponent<Renderer>();
        _renderer.materials[0].SetTexture("_TestTry3d", testTexture);
        _renderer.materials[0].SetVector("_BoundsMin", _renderer.bounds.min);
        _renderer.materials[0].SetVector("_BoundsMax", _renderer.bounds.max);
    }
}
