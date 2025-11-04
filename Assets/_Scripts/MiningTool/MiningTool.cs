using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class MiningTool : MonoBehaviour
{
    private AttackType GetAttackType() => _currentAttackType;
    private Animator GetAnimator() => _animator;
    private PlayerAndToolStats Stats => _stats;

    [Inject] private readonly LookAtTargetDetector _lookAtTargetDetector;
    [Inject] private readonly InputActions _inputActions;
    [Inject] private readonly PlayerAndToolStats _stats;

    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _transform;
    [SerializeField] private float _secondaryDamagePercent = 20f;

    private State _currentState;
    private AttackType _currentAttackType = AttackType.DurabilityDamage;
    private Coroutine _turnCoroutine;

    private void OnEnable()
    {
        _inputActions.Gameplay.LMB.performed += OnLMB;
        _inputActions.Gameplay.RMB.performed += OnRMB;
    }

    private void Start()
    {
        SetState(new IdleState(this));
    }

    private void OnDisable()
    {
        _inputActions.Gameplay.LMB.performed -= OnLMB;
        _inputActions.Gameplay.RMB.performed -= OnRMB;
    }

    private void SetState(State newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    private void SetAttackType(AttackType type)
    {
        _currentAttackType = type;
    }

    private void OnLMB(InputAction.CallbackContext ctx)
    {
        _currentState?.HandleAttack();
    }

    private void OnRMB(InputAction.CallbackContext ctx)
    {
        _currentState?.HandleTurn();
    }

    private void ToolTurn()
    {
        if (_turnCoroutine != null)
            StopCoroutine(_turnCoroutine);
        _turnCoroutine = StartCoroutine(TurnCoroutine(180f, 0.5f));
    }
    private IEnumerator TurnCoroutine(float angleDegrees, float duration)
    {
        Quaternion startRotation = _transform.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, angleDegrees, 0);
        Debug.Log("Start TurnCoroutine");

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            Debug.Log($"t = {t}, localRotation = {_transform.localRotation.eulerAngles}");
            yield return null;
        }
        _transform.localRotation = endRotation;
        _turnCoroutine = null;
        _animator.SetBool("TurnBool", false);
        OnAnimationFinished();
    }
    public void OnHitMoment()
    {
        if (_currentAttackType == AttackType.StabilityDamage)
            TryMine(_stats.MiningToolStabilityDamage * _secondaryDamagePercent / 100, _stats.MiningToolStabilityDamage);
        else
            TryMine(_stats.MiningToolDestructionDamage, _stats.MiningToolDestructionDamage * _secondaryDamagePercent / 100);
    }

    public void OnAnimationFinished()
    {
        _currentState?.OnAnimationFinished();
    }

    private void TryMine(float destructionDamage, float stabilityDamage)
    {
        if (_lookAtTargetDetector.TryRaycast(out Ray ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent<IMinable>(out var mineable))
            {
                //mineable.ApplyStabilityDamage(stabilityDamage);
                //mineable.ApplyDurabilityDamage(destructionDamage, hit.point, ray.direction.normalized);
                //Debug.Log($"{nameof(destructionDamage)} = {destructionDamage}");
                //Debug.Log($"{nameof(stabilityDamage)} = {stabilityDamage}");
            }
            if (hit.collider.TryGetComponent<IVoxelDamageable>(out var voxelDamageable))
            {
                voxelDamageable.ApplyVoxelDamage(hit.point, _stats.MiningToolRadius, stabilityDamage, destructionDamage);
                Debug.Log($"Mesh {nameof(destructionDamage)} = {destructionDamage}");
                Debug.Log($"Mesh {nameof(stabilityDamage)} = {stabilityDamage}");
            }
        }
    }

    private abstract class State
    {
        protected readonly MiningTool Tool;
        protected State(MiningTool tool) { Tool = tool; }
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void HandleAttack() { }
        public virtual void HandleTurn() { }
        public virtual void OnAnimationFinished() { }
    }

    class IdleState : State
    {
        public IdleState(MiningTool tool) : base(tool) { }

        public override void HandleAttack()
        {
            Tool.SetState(new AttackingState(Tool));
        }

        public override void HandleTurn()
        {
            Tool.SetState(new TurningState(Tool));
        }
    }

    class AttackingState : State
    {
        bool _animationFinished = false;

        public AttackingState(MiningTool tool) : base(tool) { }

        public override void Enter()
        {
            Tool.GetAnimator().SetFloat("AttackSpeed", Tool.Stats.MiningToolSpeed);
            Tool.GetAnimator().SetTrigger("AttackTrigger");
            _animationFinished = false;
        }
        public override void HandleTurn()
        {
            if(_animationFinished)
                Tool.SetState(new TurningState(Tool));
        }
        public override void OnAnimationFinished()
        {
            _animationFinished = true;
            Tool.SetState(new IdleState(Tool));
        }

    }

    class TurningState : State
    {
        bool _animationFinished = false;

        public TurningState(MiningTool tool) : base(tool) { }

        public override void Enter()
        {
            if (Tool.GetAttackType() == AttackType.StabilityDamage)
                Tool.SetAttackType(AttackType.DurabilityDamage);
            else
                Tool.SetAttackType(AttackType.StabilityDamage);
            Tool.ToolTurn();
            Tool.GetAnimator().SetBool("TurnBool", true);
            _animationFinished = false;
        }

        public override void HandleAttack()
        {
            if (_animationFinished)
            {
                Tool.SetState(new AttackingState(Tool));
            }
        }
        public override void OnAnimationFinished()
        {
            _animationFinished = true;
            Tool.SetState(new IdleState(Tool));
        }
    }

    enum AttackType
    {
        StabilityDamage,
        DurabilityDamage
    }
}
