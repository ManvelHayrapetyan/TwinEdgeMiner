using UnityEngine;

public interface IVoxelDamageable
{
    void ApplyVoxelDamage(Vector3 worldPosition, float radius, float stabilityDamage, float durabilityDamage);
}