using UnityEngine;

public class OreGenerator : MonoBehaviour
{
    [SerializeField] private GameObject _orePrefab;
    [SerializeField] private VoxelChunkManager voxelChunkManager;
    [SerializeField] private int _oreCount = 50;
    [SerializeField] private float _spawnRadius = 20f;

    private int _index = 0;

    private void Start()
    {
        // Each spawned ore registers its occupied voxels in the voxel chunks.
        for (int i = 0; i < _oreCount; i++)
        {
            _index++;
            Vector3 randomOffset = new(
                Random.Range(0, _spawnRadius),
                Random.Range(0, _spawnRadius),
                Random.Range(0, _spawnRadius));

            GameObject go = Instantiate(_orePrefab, transform.position + randomOffset, Quaternion.identity, transform);
            OreMineable oreMinable = go.GetComponent<OreMineable>();
            voxelChunkManager.OreGroundInitialize(oreMinable, _index);
        }
    }
}
