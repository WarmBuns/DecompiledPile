using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.FalseSon;

public class LunarSpikesShotgun : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public float procCoefficient;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force = 20f;

	public static float attackSpeedAltAnimationThreshold;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string attackSoundString;

	[SerializeField]
	public float attackSoundPitch;

	public static float bloom;

	private float duration;

	private bool hasFiredSpike;

	private string muzzleString;

	private Transform muzzleTransform;

	private Animator animator;

	private ChildLocator childLocator;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSoundPitch);
		base.characterBody.SetAimTimer(2f);
		animator = GetModelAnimator();
		if ((bool)animator)
		{
			childLocator = animator.GetComponent<ChildLocator>();
		}
		muzzleString = "MuzzleRight";
		if (attackSpeedStat < attackSpeedAltAnimationThreshold)
		{
			PlayCrossfade("Gesture, Additive", "FireLunarSpike", "LunarSpike.playbackRate", duration, 0.1f);
			PlayCrossfade("Gesture, Override", "FireLunarSpike", "LunarSpike.playbackRate", duration, 0.1f);
			FireLunarSpike();
		}
		else
		{
			PlayAnimation("Gesture, Override", "HoldGauntletsUp", "LunarSpike.playbackRate", duration);
			FireLunarSpike();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void FireLunarSpike()
	{
		if (!hasFiredSpike)
		{
			base.characterBody.AddSpreadBloom(bloom);
			hasFiredSpike = true;
			Ray ray = GetAimRay();
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
			if ((bool)childLocator)
			{
				muzzleTransform = childLocator.FindChild(muzzleString);
			}
			if ((bool)muzzleflashEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
			}
			if (base.isAuthority)
			{
				ProjectileManager.instance.FireProjectile(projectilePrefab, ray.origin, Util.QuaternionSafeLookRotation(ray.direction), base.gameObject, damageCoefficient * damageStat, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Void);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
