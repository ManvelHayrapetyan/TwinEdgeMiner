using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAndToolBaseStatsSO", menuName = "PlayerAndToolBaseStatsSO")]
public class PlayerAndToolBaseStatsSO : ScriptableObject
{
    public int PlayerStaminaBase => _playerStaminaBase;
    public int PlayerInventorySizeBase => _playerInventorySizeBase ;
    public float MiningToolSpeedBase => _miningToolSpeedBase ;
    public float MiningToolDestructionDamageBase => _miningToolDestructionDamageBase ;
    public float MiningToolStabilityDamageBase  => _miningToolStabilityDamageBase ;

    [SerializeField] private int _playerStaminaBase;
    [SerializeField] private int _playerInventorySizeBase;
    [SerializeField] private float _miningToolSpeedBase;
    [SerializeField] private float _miningToolDestructionDamageBase;
    [SerializeField] private float _miningToolStabilityDamageBase;

}
