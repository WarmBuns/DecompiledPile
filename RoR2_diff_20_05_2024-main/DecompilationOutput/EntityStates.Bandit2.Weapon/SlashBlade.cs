using RoR2;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public class SlashBlade : BasicMeleeAttack
{
	public static float shortHopVelocity;

	public static float selfForceStrength;

	public static float minimumBaseDuration;

	public static AnimationCurve bloomCurve;

	private GameObject bladeMeshObject;

	private static int SlashBladeStateHash = Animator.StringToHash("SlashBlade");

	private static int SlashBladeParamHash = Animator.StringToHash("SlashBlade.playbackRate");

	private float minimumDuration => minimumBaseDuration / attackSpeedStat;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Gesture, Additive", SlashBladeStateHash, SlashBladeParamHash, duration);
		bladeMeshObject = FindModelChild("BladeMesh").gameObject;
		if ((bool)bladeMeshObject)
		{
			bladeMeshObject.SetActive(value: true);
		}
		base.characterMotor.ApplyForce(base.inputBank.moveVector * selfForceStrength, alwaysApply: true);
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity = new Vector3(base.characterMotor.velocity.x, Mathf.Max(base.characterMotor.velocity.y, shortHopVelocity), base.characterMotor.velocity.z);
		}
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
		overlapAttack.damageType = DamageType.SuperBleedOnCrit;
	}

	public override void Update()
	{
		base.Update();
		base.characterBody.SetSpreadBloom(bloomCurve.Evaluate(base.age / duration), canOnlyIncreaseBloom: false);
	}

	public override void OnExit()
	{
		if ((bool)bladeMeshObject)
		{
			bladeMeshObject.SetActive(value: false);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if ((bool)base.inputBank)
		{
			if (!(base.fixedAge > minimumDuration))
			{
				return InterruptPriority.PrioritySkill;
			}
			return InterruptPriority.Skill;
		}
		return InterruptPriority.Skill;
	}
}
