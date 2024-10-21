using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Mage.Weapon;

public class FireFireBolt : BaseState, SteppedSkillDef.IStepSetter
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

	private float duration;

	private bool hasFiredGauntlet;

	private string muzzleString;

	private Transform muzzleTransform;

	private Animator animator;

	private ChildLocator childLocator;

	private Gauntlet gauntlet;

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	private static int Cast1LeftStateHash = Animator.StringToHash("Cast1Left");

	private static int FireGauntletLeftStateHash = Animator.StringToHash("FireGauntletLeft");

	private static int Cast1RightStateHash = Animator.StringToHash("Cast1Right");

	private static int FireGauntletRightStateHash = Animator.StringToHash("FireGauntletRight");

	private static int HoldGauntletsUpStateHash = Animator.StringToHash("HoldGauntletsUp");

	private static int FireGauntletParamHash = Animator.StringToHash("FireGauntlet.playbackRate");

	public void SetStep(int i)
	{
		gauntlet = (Gauntlet)i;
	}

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
		switch (gauntlet)
		{
		case Gauntlet.Left:
			muzzleString = "MuzzleLeft";
			if (attackSpeedStat < attackSpeedAltAnimationThreshold)
			{
				PlayCrossfade("Gesture, Additive", Cast1LeftStateHash, FireGauntletParamHash, duration, 0.1f);
				PlayAnimation("Gesture Left, Additive", EmptyStateHash);
				PlayAnimation("Gesture Right, Additive", EmptyStateHash);
			}
			else
			{
				PlayAnimation("Gesture Left, Additive", FireGauntletLeftStateHash, FireGauntletParamHash, duration);
				PlayAnimation("Gesture, Additive", HoldGauntletsUpStateHash, FireGauntletParamHash, duration);
				FireGauntlet();
			}
			break;
		case Gauntlet.Right:
			muzzleString = "MuzzleRight";
			if (attackSpeedStat < attackSpeedAltAnimationThreshold)
			{
				PlayCrossfade("Gesture, Additive", Cast1RightStateHash, FireGauntletParamHash, duration, 0.1f);
				PlayAnimation("Gesture Left, Additive", EmptyStateHash);
				PlayAnimation("Gesture Right, Additive", EmptyStateHash);
			}
			else
			{
				PlayAnimation("Gesture Right, Additive", FireGauntletRightStateHash, FireGauntletParamHash, duration);
				PlayAnimation("Gesture, Additive", HoldGauntletsUpStateHash, FireGauntletParamHash, duration);
				FireGauntlet();
			}
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
			hasFiredGauntlet = true;
			Ray ray = GetAimRay();
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
				TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
				ProjectileManager.instance.FireProjectile(projectilePrefab, ray.origin, Util.QuaternionSafeLookRotation(ray.direction), base.gameObject, damageCoefficient * damageStat, 0f, Util.CheckRoll(critStat, base.characterBody.master));
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (animator.GetFloat("FireGauntlet.fire") > 0f && !hasFiredGauntlet)
		{
			FireGauntlet();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
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
