using UnityEngine;

public class CubMeshTest : MonoBehaviour
{
    private Renderer _renderer;
    void Start()
    {
        var textry = new Texture3D(16, 16, 16, TextureFormat.RGBA32, false);
        textry.wrapMode = TextureWrapMode.Repeat;
        textry.filterMode = FilterMode.Point;

        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                for (int z = 0; z < 16; z++)
                {
                    textry.SetPixel(x, y, z, new Color(0, y / 15f, z / 15f, 1f));
                }

        textry.Apply();
        _renderer = GetComponent<Renderer>();
        _renderer.materials[0].SetTexture("_TestTry3d", textry);
        //Vector3 localBoundsMin = transform.InverseTransformPoint();
        //Vector3 localBoundsMax = transform.InverseTransformPoint();
        _renderer.materials[0].SetVector("_BoundsMin", _renderer.bounds.min);
        _renderer.materials[0].SetVector("_BoundsMax", _renderer.bounds.max);
    }
}