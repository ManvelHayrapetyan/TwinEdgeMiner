using UnityEngine;
using Zenject;

public class OreGenerator : MonoBehaviour
{
    [SerializeField] private GameObject _orePrefab;
    [SerializeField] private VoxelChunkManager voxelChunkManager;
    [SerializeField] private int _oreCount = 50; 
    [SerializeField] private float _spawnRadius = 20f; 
    private int _index = 0;
    private void Start()
    {
        //GameObject go = Instantiate(_orePrefab, transform.position, Quaternion.identity, transform);
        //OreMineable oreMinable = go.GetComponent<OreMineable>();
        //voxelChunkManager.OreGroundInitialize(oreMinable, _index);

        for (int i = 0; i < _oreCount; i++)
        {
            _index++;
            Vector3 randomOffset = new Vector3(
                Random.Range(0, _spawnRadius),
                Random.Range(0, _spawnRadius),
                Random.Range(0, _spawnRadius)
            );

            Vector3 spawnPos = transform.position + randomOffset;

            GameObject go = Instantiate(_orePrefab, spawnPos, Quaternion.identity, transform);
            OreMineable oreMinable = go.GetComponent<OreMineable>();
            voxelChunkManager.OreGroundInitialize(oreMinable, _index);
        }
    }
}