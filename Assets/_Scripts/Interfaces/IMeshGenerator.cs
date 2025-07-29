using UnityEngine;

public interface IMeshGenerator
{
    Mesh GenerateMesh(IVoxelData voxelData);
}