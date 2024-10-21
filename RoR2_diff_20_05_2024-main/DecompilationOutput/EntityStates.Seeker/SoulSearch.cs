using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Seeker;

public class SoulSearch : BaseState, SteppedSkillDef.IStepSetter
{
	public enum Gauntlet
	{
		Left,
		Right
	}

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

	[SerializeField]
	public GameObject blastEffectPrefab;

	[SerializeField]
	public GameObject blastImpactEffectPrefab;

	[SerializeField]
	public float blastRadius;

	[SerializeField]
	public float blastDamageCoefficient;

	[SerializeField]
	public float blastForce;

	[SerializeField]
	public Vector3 blastBonusForce;

	[SerializeField]
	public float blastProcCoefficient;

	private float duration;

	private bool hasFiredGauntlet;

	private string muzzleString;

	private Transform muzzleTransform;

	private Animator animator;

	private ChildLocator childLocator;

	private Gauntlet gauntlet;

	private HuntressTracker huntressTracker;

	public void SetStep(int i)
	{
		gauntlet = (Gauntlet)i;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		huntressTracker = base.characterBody.GetComponent<HuntressTracker>();
		if (!huntressTracker.GetTrackingTarget())
		{
			outer.SetNextStateToMain();
			return;
		}
		duration = baseDuration / attackSpeedStat;
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSoundPitch);
		base.characterBody.SetAimTimer(2f);
		animator = GetModelAnimator();
		if ((bool)animator)
		{
			childLocator = animator.GetComponent<ChildLocator>();
		}
		switch (gauntlet)
		{
		case Gauntlet.Left:
			muzzleString = "MuzzleLeft";
			PlayCrossfade("Gesture, Additive", "Cast1Left", "FireGauntlet.playbackRate", duration, 0.1f);
			PlayCrossfade("Gesture, Override", "Cast1Left", "FireGauntlet.playbackRate", duration, 0.1f);
			break;
		case Gauntlet.Right:
			muzzleString = "MuzzleRight";
			PlayCrossfade("Gesture, Additive", "Cast1Right", "FireGauntlet.playbackRate", duration, 0.1f);
			PlayCrossfade("Gesture, Override", "Cast1Right", "FireGauntlet.playbackRate", duration, 0.1f);
			break;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void FireGauntlet()
	{
		if (!hasFiredGauntlet)
		{
			base.characterBody.AddSpreadBloom(bloom);
			Ray aimRay = GetAimRay();
			if ((bool)childLocator)
			{
				muzzleTransform = childLocator.FindChild(muzzleString);
			}
			if ((bool)muzzleflashEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
			}
			if (projectilePrefab != null && base.isAuthority)
			{
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.projectilePrefab = projectilePrefab;
				fireProjectileInfo.position = muzzleTransform.position;
				fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.damage = damageStat * damageCoefficient;
				FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
				ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration || hasFiredGauntlet)
		{
			if (!hasFiredGauntlet && (bool)huntressTracker.GetTrackingTarget())
			{
				FireGauntlet();
				hasFiredGauntlet = true;
			}
			if (base.isAuthority)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)gauntlet);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		gauntlet = (Gauntlet)reader.ReadByte();
	}
}
