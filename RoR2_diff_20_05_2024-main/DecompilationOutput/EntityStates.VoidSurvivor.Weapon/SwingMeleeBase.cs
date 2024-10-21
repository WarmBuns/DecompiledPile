using RoR2;
using UnityEngine;

namespace EntityStates.VoidSurvivor.Weapon;

public class SwingMeleeBase : BasicMeleeAttack
{
	[SerializeField]
	public float recoilAmplitude;

	[SerializeField]
	public float bloom;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParameter;

	protected override bool allowExitFire
	{
		get
		{
			if ((bool)base.characterBody)
			{
				return !base.characterBody.isSprinting;
			}
			return false;
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		base.characterDirection.forward = GetAimRay().direction;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
	}

	protected override void PlayAnimation()
	{
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParameter, duration);
	}

	protected override void OnMeleeHitAuthority()
	{
		base.OnMeleeHitAuthority();
		base.characterBody.AddSpreadBloom(bloom);
	}

	protected override void BeginMeleeAttackEffect()
	{
		AddRecoil(-0.1f * recoilAmplitude, 0.1f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		base.BeginMeleeAttackEffect();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
