using UnityEngine;

public interface IMinable
{
    void ApplyDamage(Vector3 hitPoint, float stabilityDamage, float durabilityDamage);
}