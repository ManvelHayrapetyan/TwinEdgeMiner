using UnityEngine;

public interface IMinable
{
    void ApplyStabilityDamage(float amount);
    void ApplyDurabilityDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);

}