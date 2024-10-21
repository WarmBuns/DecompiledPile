using RoR2.UI;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public class BaseSidearmState : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public GameObject crosshairOverridePrefab;

	protected float duration;

	private Animator animator;

	private int bodySideWeaponLayerIndex;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	public virtual string exitAnimationStateName => "BufferEmpty";

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		duration = baseDuration / attackSpeedStat;
		if ((bool)animator)
		{
			bodySideWeaponLayerIndex = animator.GetLayerIndex("Body, SideWeapon");
			animator.SetLayerWeight(bodySideWeaponLayerIndex, 1f);
		}
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
		base.characterBody.SetAimTimer(3f);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.characterBody.isSprinting)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if ((bool)animator)
		{
			animator.SetLayerWeight(bodySideWeaponLayerIndex, 0f);
		}
		PlayAnimation("Gesture, Additive", exitAnimationStateName);
		crosshairOverrideRequest?.Dispose();
		Transform transform = FindModelChild("SpinningPistolFX");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: false);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
