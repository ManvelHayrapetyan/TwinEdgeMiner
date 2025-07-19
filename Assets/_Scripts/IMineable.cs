public interface IMineable
{
    float Stability { get; }
    float Durability { get; }
    void ApplyStabilityDamage(float amount);
    void ApplyDurabilityDamage(float amount);
}