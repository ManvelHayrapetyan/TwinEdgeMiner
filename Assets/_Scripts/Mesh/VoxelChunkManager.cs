using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;

public class VoxelChunkManager : MonoBehaviour
{
    private static readonly Unity.Profiling.ProfilerMarker ApplyDamageMarker = new("Voxel.Manager.ApplyDamage");
    private static readonly Unity.Profiling.ProfilerMarker GetAffectedChunksMarker = new("Voxel.Manager.GetAffectedChunks");
    private static readonly Unity.Profiling.ProfilerMarker ChunkDamageJobsMarker = new("Voxel.Manager.ChunkDamageJobs");
    private static readonly Unity.Profiling.ProfilerMarker OreDamageMarker = new("Voxel.Manager.OreDamage");
    private static readonly Unity.Profiling.ProfilerMarker GetUpdateChunksMarker = new("Voxel.Manager.GetUpdateChunks");
    private static readonly Unity.Profiling.ProfilerMarker PaddingUpdateMarker = new("Voxel.Manager.PaddingUpdate");
    private static readonly Unity.Profiling.ProfilerMarker PaddingSwapMarker = new("Voxel.Manager.PaddingSwap");
    private static readonly Unity.Profiling.ProfilerMarker UpdateChunksMarker = new("Voxel.Manager.UpdateChunks");
    private static readonly Unity.Profiling.ProfilerMarker FillPaddedDensityMarker = new("Voxel.Manager.FillPaddedDensity");
    [SerializeField] private int _chunkCountX = 8;
    [SerializeField] private int _chunkCountY = 8;
    [SerializeField] private int _chunkCountZ = 8;
    [SerializeField, Min(2)] private int _voxelsPerChunk = 8;
    [SerializeField] private float _voxelSize = 0.5f;
    [SerializeField] private float _maxStability = 0f;
    [SerializeField] private float _maxDurability = 40f;
    [SerializeField] private float _alpha = 30f;
    [SerializeField] private GameObject _chunkPrefab;

    private int _index = 0;
    private Vector3 _chunkWorldSize;

    private readonly Dictionary<Vector3Int, VoxelChunk> _chunkDict = new();
    private readonly Dictionary<Vector3Int, VoxelChunkRenderer> _chunkRendererDict = new();
    private readonly Dictionary<int, List<Vector3Int>> _oreToChunkList = new();
    private readonly Dictionary<int, OreMineable> _oreInstances = new();
    private void Awake()
    {
        _chunkWorldSize = Vector3.one * _voxelsPerChunk * _voxelSize;
        CreateChunks();
    }


    public void ApplyDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
        using Unity.Profiling.ProfilerMarker.AutoScope _ = ApplyDamageMarker.Auto();

        HashSet<Vector3Int> affectedChunks;
        using (GetAffectedChunksMarker.Auto())
        {
            affectedChunks = GetAffectedChunks(worldPosition, radius);
        }

        HashSet<Vector3Int> affectedOreChunks = new();
        HashSet<int> oreUniqueIndexes = new();
        Dictionary<Vector3Int, NativeArray<Color32>> indexToColor = new();

        using (ChunkDamageJobsMarker.Auto())
        {
            foreach (Vector3Int chunkPos in affectedChunks)
            {
                if (_chunkDict.TryGetValue(chunkPos, out VoxelChunk chunk))
                {
                    int[] oreIndexes = chunk.ApplyDamage(
                        WorldPosToLocalChunkPos(worldPosition, chunkPos),
                        radius, stabilityDamage, durabilityDamage);

                    foreach (int index in oreIndexes)
                        oreUniqueIndexes.Add(index);
                }
            }
        }

        using (OreDamageMarker.Auto())
        {
            foreach (int oreIndex in oreUniqueIndexes)
            {
                OreDamageResult oreDamageResult = _oreInstances[oreIndex].ApplyDamage(stabilityDamage, durabilityDamage);

                foreach (Vector3Int chunkIndex in _oreToChunkList[oreIndex])
                {
                    switch (oreDamageResult)
                    {
                        case OreDamageResult.None:
                            break;

                        case OreDamageResult.CrackChanged:
                            indexToColor[chunkIndex] =
                            _chunkDict[chunkIndex].ApplyCrack(
                                WorldPosToLocalChunkPos(worldPosition, chunkIndex),
                                WorldPosToLocalChunkPos(_oreInstances[oreIndex].Center, chunkIndex),
                                _oreInstances[oreIndex].Stability,
                                _oreInstances[oreIndex].MaxStability,
                                oreIndex);
                            affectedOreChunks.Add(chunkIndex);
                            break;

                        case OreDamageResult.LayerDestroyed:
                            _chunkDict[chunkIndex].DestroyOreShellLayer(
                                WorldPosToLocalChunkPos(worldPosition, chunkIndex),
                                WorldPosToLocalChunkPos(_oreInstances[oreIndex].Center, chunkIndex),
                                oreIndex);
                            affectedOreChunks.Add(chunkIndex);
                            break;

                        case OreDamageResult.FullyMined:
                            _chunkDict[chunkIndex].DestroyAllOreVoxels(oreIndex);
                            affectedOreChunks.Add(chunkIndex);
                            break;
                    }
                }
            }
        }

        affectedChunks.UnionWith(affectedOreChunks);

        HashSet<Vector3Int> chunksToUpdate;
        using (GetUpdateChunksMarker.Auto())
        {
            chunksToUpdate = GetAffectedChunksWithNeighbors(affectedChunks);
        }

        using (PaddingUpdateMarker.Auto())
        {
            foreach (var chunkToUpdate in chunksToUpdate)
                ChunkNeighborsPaddingSet(chunkToUpdate);
        }

        using (PaddingSwapMarker.Auto())
        {
            foreach (Vector3Int chunkToUpdate in chunksToUpdate)
                _chunkDict[chunkToUpdate].Padding.SwapAll();
        }

        using (UpdateChunksMarker.Auto())
        {
            UpdateChunks(chunksToUpdate, indexToColor);
        }
    }
    private Vector3 WorldPosToLocalChunkPos(Vector3 WorldPos, Vector3Int index)
    {
        return WorldPos - transform.position - Vector3.Scale((Vector3)index, _chunkWorldSize);
    }

    private void UpdateChunks(IEnumerable<Vector3Int> indexes, Dictionary<Vector3Int, NativeArray<Color32>> indexToColor)
    {
        foreach (Vector3Int index in indexes)
        {
            _chunkRendererDict[index].UpdateGO(_chunkDict[index]);
            _chunkRendererDict[index].UpdateMesh();
            if (indexToColor.TryGetValue(index, out NativeArray<Color32> color))
                _chunkRendererDict[index].UpdateGO(_chunkDict[index], color);
        }
    }

    public void OreGroundInitialize(OreMineable oreMinable, int index)
    {
        _index++;
        _oreInstances[index] = oreMinable;
        Vector3 center = oreMinable.Center;
        float radius = oreMinable.Radius;
        HashSet<Vector3Int> affectedChunks = GetAffectedChunks(center, radius);

        foreach (Vector3Int chunkPos in affectedChunks)
            if (_chunkDict.TryGetValue(chunkPos, out VoxelChunk chunk))
            {
                chunk.OreGroundInitialize(WorldPosToLocalChunkPos(center, chunkPos), radius, index);
                if (!_oreToChunkList.TryGetValue(index, out _))
                    _oreToChunkList[index] = new List<Vector3Int>();
                _oreToChunkList[index].Add(chunkPos);
            }
    }

    private HashSet<Vector3Int> GetAffectedChunks(Vector3 center, float radius)
    {
        Vector3 localCenter = center - transform.position;
        HashSet<Vector3Int> result = new();
        int minX = Mathf.Max(0, Mathf.FloorToInt((localCenter.x - radius) / _chunkWorldSize.x));
        int maxX = Mathf.Min(_chunkCountX - 1, Mathf.CeilToInt((localCenter.x + radius) / _chunkWorldSize.x));
        int minY = Mathf.Max(0, Mathf.FloorToInt((localCenter.y - radius) / _chunkWorldSize.y));
        int maxY = Mathf.Min(_chunkCountY - 1, Mathf.CeilToInt((localCenter.y + radius) / _chunkWorldSize.y));
        int minZ = Mathf.Max(0, Mathf.FloorToInt((localCenter.z - radius) / _chunkWorldSize.z));
        int maxZ = Mathf.Min(_chunkCountZ - 1, Mathf.CeilToInt((localCenter.z + radius) / _chunkWorldSize.z));

        float sqrRadius = radius * radius;
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3 chunkMin = Vector3.Scale(new Vector3(x, y, z), _chunkWorldSize);
                    Vector3 chunkMax = chunkMin + _chunkWorldSize;

                    if (SphereIntersectsAABB(localCenter, radius, chunkMin, chunkMax))
                        result.Add(new Vector3Int(x, y, z));
                }

        return result;
    }

    private bool SphereIntersectsAABB(Vector3 center, float radius, Vector3 min, Vector3 max)
    {
        Vector3 closest = Vector3.Max(min, Vector3.Min(center, max));
        return (closest - center).sqrMagnitude <= radius * radius;
    }

    private void CreateChunks()
    {
        for (int x = 0; x < _chunkCountX; x++)
            for (int y = 0; y < _chunkCountY; y++)
                for (int z = 0; z < _chunkCountZ; z++)
                {
                    Vector3Int chunkPos = new(x, y, z);
                    Vector3 worldPos = Vector3.Scale(chunkPos, _chunkWorldSize);


                    GameObject go = Instantiate(_chunkPrefab, transform.position + worldPos, Quaternion.identity, transform);
                    go.name = $"Chunk_{x}_{y}_{z}";
                    VoxelChunkRenderer chunkRenderer = go.GetComponent<VoxelChunkRenderer>();

                    VoxelChunk chunkData = new(_voxelsPerChunk, _voxelSize, _maxStability, _maxDurability, _alpha);
                    chunkData.ApplyWorldBoundaries(
                        x == 0, x == _chunkCountX - 1,
                        y == 0, y == _chunkCountY - 1,
                        z == 0, z == _chunkCountZ - 1);
                    _chunkDict[chunkPos] = chunkData;
                    _chunkRendererDict[chunkPos] = chunkRenderer;
                }

        for (int x = 0; x < _chunkCountX; x++)
            for (int y = 0; y < _chunkCountY; y++)
                for (int z = 0; z < _chunkCountZ; z++)
                {
                    ChunkNeighborsPaddingSet(new Vector3Int(x, y, z));
                }

        for (int x = 0; x < _chunkCountX; x++)
            for (int y = 0; y < _chunkCountY; y++)
                for (int z = 0; z < _chunkCountZ; z++)
                {
                    if (_chunkDict.TryGetValue(new Vector3Int(x, y, z), out VoxelChunk chunk))
                        chunk.Padding.SwapAll();
                }

        for (int x = 0; x < _chunkCountX; x++)
            for (int y = 0; y < _chunkCountY; y++)
                for (int z = 0; z < _chunkCountZ; z++)
                {
                    ChunkNeighborsPaddingSet(new Vector3Int(x, y, z));
                }


        for (int x = 0; x < _chunkCountX; x++)
            for (int y = 0; y < _chunkCountY; y++)
                for (int z = 0; z < _chunkCountZ; z++)
                {
                    if (_chunkDict.TryGetValue(new Vector3Int(x, y, z), out VoxelChunk chunk))
                    {
                        _chunkRendererDict[new Vector3Int(x, y, z)].Init(_chunkDict[new Vector3Int(x, y, z)]);
                        _chunkRendererDict[new Vector3Int(x, y, z)].UpdateMesh();

                    }
                }
    }

    private void ChunkNeighborsPaddingSet(Vector3Int chunkID)
    {
        if (!_chunkDict.ContainsKey(chunkID))
            return;

        FillPaddedDensity(chunkID);
    }

    private void FillPaddedDensity(Vector3Int chunkID)
    {
        VoxelChunk targetChunk = _chunkDict[chunkID];
        VoxelChunkPadding padding = targetChunk.Padding;
        int paddingSize = padding.PaddingSize;

        padding.ClearNext();

        for (int offsetX = -1; offsetX <= 1; offsetX++)
            for (int offsetY = -1; offsetY <= 1; offsetY++)
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    Vector3Int sourceChunkID = chunkID + new Vector3Int(offsetX, offsetY, offsetZ);
                    if (!_chunkDict.TryGetValue(sourceChunkID, out VoxelChunk sourceChunk))
                        continue;

                    GetPaddingCopyRange(offsetX, paddingSize, out int sourceStartX, out int destinationStartX, out int sizeX);
                    GetPaddingCopyRange(offsetY, paddingSize, out int sourceStartY, out int destinationStartY, out int sizeY);
                    GetPaddingCopyRange(offsetZ, paddingSize, out int sourceStartZ, out int destinationStartZ, out int sizeZ);

                    padding.CopyDensityBlockFrom(
                        sourceChunk.Dencity,
                        _voxelsPerChunk,
                        sourceStartX,
                        sourceStartY,
                        sourceStartZ,
                        destinationStartX,
                        destinationStartY,
                        destinationStartZ,
                        sizeX,
                        sizeY,
                        sizeZ);
                }
    }

    private void GetPaddingCopyRange(
        int chunkOffset,
        int paddingSize,
        out int sourceStart,
        out int destinationStart,
        out int size)
    {
        if (chunkOffset < 0)
        {
            sourceStart = _voxelsPerChunk - paddingSize;
            destinationStart = 0;
            size = paddingSize;
            return;
        }

        if (chunkOffset > 0)
        {
            sourceStart = 0;
            destinationStart = paddingSize + _voxelsPerChunk;
            size = paddingSize;
            return;
        }

        sourceStart = 0;
        destinationStart = paddingSize;
        size = _voxelsPerChunk;
    }

    private void OnDestroy()
    {
        for (int x = 0; x < _chunkCountX; x++)
            for (int y = 0; y < _chunkCountY; y++)
                for (int z = 0; z < _chunkCountZ; z++)
                {
                    if (_chunkDict.TryGetValue(new Vector3Int(x, y, z), out VoxelChunk chunk))
                        chunk?.Dispose();
                }
    }

    private HashSet<Vector3Int> GetAffectedChunksWithNeighbors(HashSet<Vector3Int> affectedChunks)
    {
        HashSet<Vector3Int> result = new(affectedChunks);

        foreach (Vector3Int chunkID in affectedChunks)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                            continue;

                        TryAddChunk(result, chunkID + new Vector3Int(x, y, z));
                    }
        }

        return result;
    }

    private void TryAddChunk(HashSet<Vector3Int> chunks, Vector3Int chunkID)
    {
        if (_chunkDict.ContainsKey(chunkID))
            chunks.Add(chunkID);
    }
}
















