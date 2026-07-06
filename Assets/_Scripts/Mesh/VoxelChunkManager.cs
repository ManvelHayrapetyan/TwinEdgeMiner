using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class VoxelChunkManager : MonoBehaviour
{
    [SerializeField] private int _chunkCountX = 8;
    [SerializeField] private int _chunkCountY = 8;
    [SerializeField] private int _chunkCountZ = 8;
    [SerializeField] private int _voxelsPerChunkX = 8;
    [SerializeField] private int _voxelsPerChunkY = 8;
    [SerializeField] private int _voxelsPerChunkZ = 8;
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
        _chunkWorldSize = new Vector3(_voxelsPerChunkX, _voxelsPerChunkY, _voxelsPerChunkZ) * _voxelSize;
        CreateChunks();
    }


    public void ApplyDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage)
    {
        HashSet<Vector3Int> affectedChunks = GetAffectedChunks(worldPosition, radius);
        HashSet<Vector3Int> affectedOreChunks = new();
        HashSet<int> oreUniqueIndexes = new();
        Dictionary<Vector3Int, NativeArray<Color32>> indexToColor = new();
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
        affectedChunks.UnionWith(affectedOreChunks);
        foreach (var affectedChunk in affectedChunks)
        {
            ChunkNeighborsPaddingSet(affectedChunk);
        }

        for (int x = 0; x < _chunkCountX; x++)
            for (int y = 0; y < _chunkCountY; y++)
                for (int z = 0; z < _chunkCountZ; z++)
                {
                    if (_chunkDict.TryGetValue(new Vector3Int(x, y, z), out VoxelChunk chunk))
                        chunk.Padding.SwapAll();
                }

        UpdateChunks(GetAffectedChunksWithNeighbors(affectedChunks).ToArray(), indexToColor);
    }

    private Vector3 WorldPosToLocalChunkPos(Vector3 WorldPos, Vector3Int index)
    {
        return WorldPos - transform.position - Vector3.Scale((Vector3)index, _chunkWorldSize);
    }

    private void UpdateChunks(Vector3Int[] indexes, Dictionary<Vector3Int, NativeArray<Color32>> indexToColor)
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
        Debug.Log($"aaaa {_index}, {index}");
        _oreInstances[index] = oreMinable;
        Vector3 center = oreMinable.Center;
        float radius = oreMinable.Radius;
        HashSet<Vector3Int> affectedChunks = GetAffectedChunks(center, radius);
        Debug.Log($"affectedChunks{affectedChunks.Count}");

        foreach (Vector3Int chunkPos in affectedChunks)
            if (_chunkDict.TryGetValue(chunkPos, out VoxelChunk chunk))
            {
                Debug.Log("XSASSDA");
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

                    VoxelChunk chunkData = new(_voxelsPerChunkX, _voxelsPerChunkY, _voxelsPerChunkZ, _voxelSize, _maxStability, _maxDurability, _alpha);
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
        if (!_chunkDict.TryGetValue(chunkID, out _))
            return;
        VoxelChunk chunk;

        // Face
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(1, 0, 0), out chunk))
        {
            for (int y = 0; y < _voxelsPerChunkY; y++)
                for (int z = 0; z < _voxelsPerChunkZ; z++)
                {
                    chunk.Padding.FaceXMinus.Set(y, z, _chunkDict[chunkID][_voxelsPerChunkX - 1, y, z]);
                }
        }
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(-1, 0, 0), out chunk))
        {
            for (int y = 0; y < _voxelsPerChunkY; y++)
                for (int z = 0; z < _voxelsPerChunkZ; z++)
                {
                    chunk.Padding.FaceXPlus.Set(y, z, _chunkDict[chunkID][0, y, z]);
                }
        }

        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(0, 1, 0), out chunk))
        {
            for (int x = 0; x < _voxelsPerChunkX; x++)
                for (int z = 0; z < _voxelsPerChunkZ; z++)
                {
                    chunk.Padding.FaceYMinus.Set(x, z, _chunkDict[chunkID][x, _voxelsPerChunkY - 1, z]);
                }
        }
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(0, -1, 0), out chunk))
        {
            for (int x = 0; x < _voxelsPerChunkX; x++)
                for (int z = 0; z < _voxelsPerChunkZ; z++)
                {
                    chunk.Padding.FaceYPlus.Set(x, z, _chunkDict[chunkID][x, 0, z]);
                }
        }

        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(0, 0, 1), out chunk))
        {
            for (int x = 0; x < _voxelsPerChunkX; x++)
                for (int y = 0; y < _voxelsPerChunkY; y++)
                {
                    chunk.Padding.FaceZMinus.Set(x, y, _chunkDict[chunkID][x, y, _voxelsPerChunkZ - 1]);
                }
        }
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(0, 0, -1), out chunk))
        {
            for (int x = 0; x < _voxelsPerChunkX; x++)
                for (int y = 0; y < _voxelsPerChunkY; y++)
                {
                    chunk.Padding.FaceZPlus.Set(x, y, _chunkDict[chunkID][x, y, 0]);
                }
        }
        // Edge
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(-1, -1, 0), out chunk))
        {
            for (int z = 0; z < _voxelsPerChunkZ; z++)
            {
                chunk.Padding.EdgeXPlusYPlus.Set(z, 0, _chunkDict[chunkID][0, 0, z]);
            }
        }
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(-1, 0, -1), out chunk))
        {
            for (int y = 0; y < _voxelsPerChunkY; y++)
            {
                chunk.Padding.EdgeXPlusZPlus.Set(y, 0, _chunkDict[chunkID][0, y, 0]);
            }
        }
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(0, -1, -1), out chunk))
        {
            for (int x = 0; x < _voxelsPerChunkX; x++)
            {
                chunk.Padding.EdgeYPlusZPlus.Set(x, 0, _chunkDict[chunkID][x, 0, 0]);
            }
        }
        // Corner
        if (_chunkDict.TryGetValue(chunkID + new Vector3Int(-1, -1, -1), out chunk))
        {
            chunk.Padding.CornerXPlusYPlusZPlus.Set(0, 0, _chunkDict[chunkID][0, 0, 0]);
        }
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


        void AddChunkIfExists(HashSet<Vector3Int> chunks, Vector3Int chunkID)
        {
            if (_chunkDict.ContainsKey(chunkID))
                chunks.Add(chunkID);
        }

        foreach (Vector3Int chunkID in affectedChunks)
        {
            AddChunkIfExists(result, chunkID + new Vector3Int(1, 0, 0));
            AddChunkIfExists(result, chunkID + new Vector3Int(-1, 0, 0));

            AddChunkIfExists(result, chunkID + new Vector3Int(0, 1, 0));
            AddChunkIfExists(result, chunkID + new Vector3Int(0, -1, 0));

            AddChunkIfExists(result, chunkID + new Vector3Int(0, 0, 1));
            AddChunkIfExists(result, chunkID + new Vector3Int(0, 0, -1));

            AddChunkIfExists(result, chunkID + new Vector3Int(-1, -1, 0));
            AddChunkIfExists(result, chunkID + new Vector3Int(-1, 0, -1));
            AddChunkIfExists(result, chunkID + new Vector3Int(0, -1, -1));

            AddChunkIfExists(result, chunkID + new Vector3Int(-1, -1, -1));
        }

        return result;
    }
}