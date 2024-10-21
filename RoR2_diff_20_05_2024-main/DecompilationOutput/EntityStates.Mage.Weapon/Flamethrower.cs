using RoR2;
using UnityEngine;

namespace EntityStates.Mage.Weapon;

public class Flamethrower : BaseState
{
	[SerializeField]
	public GameObject flamethrowerEffectPrefab;

	public static GameObject impactEffectPrefab;

	public static GameObject tracerEffectPrefab;

	[SerializeField]
	public float maxDistance;

	public static float radius;

	public static float baseEntryDuration = 1f;

	public static float baseFlamethrowerDuration = 2f;

	public static float totalDamageCoefficient = 1.2f;

	public static float procCoefficientPerTick;

	public static float tickFrequency;

	public static float force = 20f;

	public static string startAttackSoundString;

	public static string endAttackSoundString;

	public static float ignitePercentChance;

	public static float recoilForce;

	private float tickDamageCoefficient;

	private float flamethrowerStopwatch;

	private float stopwatch;

	private float entryDuration;

	private float flamethrowerDuration;

	private bool hasBegunFlamethrower;

	private ChildLocator childLocator;

	private Transform leftFlamethrowerTransform;

	private Transform rightFlamethrowerTransform;

	private Transform leftMuzzleTransform;

	private Transform rightMuzzleTransform;

	private bool isCrit;

	private static int PrepFlamethrowerStateHash = Animator.StringToHash("PrepFlamethrower");

	private static int ExitFlamethrowerStateHash = Animator.StringToHash("ExitFlamethrower");

	private static int FlamethrowerParamHash = Animator.StringToHash("Flamethrower.playbackRate");

	private const float flamethrowerEffectBaseDistance = 16f;

	private static int FlamethrowerStateHash = Animator.StringToHash("Flamethrower");

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		entryDuration = baseEntryDuration / attackSpeedStat;
		flamethrowerDuration = baseFlamethrowerDuration;
		Transform modelTransform = GetModelTransform();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(entryDuration + flamethrowerDuration + 1f);
		}
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			leftMuzzleTransform = childLocator.FindChild("MuzzleLeft");
			rightMuzzleTransform = childLocator.FindChild("MuzzleRight");
		}
		int num = Mathf.CeilToInt(flamethrowerDuration * tickFrequency);
		tickDamageCoefficient = totalDamageCoefficient / (float)num;
		if (base.isAuthority && (bool)base.characterBody)
		{
			isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		}
		PlayAnimation("Gesture, Additive", PrepFlamethrowerStateHash, FlamethrowerParamHash, entryDuration);
	}

	public override void OnExit()
	{
		Util.PlaySound(endAttackSoundString, base.gameObject);
		PlayCrossfade("Gesture, Additive", ExitFlamethrowerStateHash, 0.1f);
		if ((bool)leftFlamethrowerTransform)
		{
			EntityState.Destroy(leftFlamethrowerTransform.gameObject);
		}
		if ((bool)rightFlamethrowerTransform)
		{
			EntityState.Destroy(rightFlamethrowerTransform.gameObject);
		}
		base.OnExit();
	}

	private void FireGauntlet(string muzzleString)
	{
		Ray aimRay = GetAimRay();
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.minSpread = 0f;
			bulletAttack.damage = tickDamageCoefficient * damageStat;
			bulletAttack.force = force;
			bulletAttack.muzzleName = muzzleString;
			bulletAttack.hitEffectPrefab = impactEffectPrefab;
			bulletAttack.isCrit = isCrit;
			bulletAttack.radius = radius;
			bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
			bulletAttack.stopperMask = LayerIndex.world.mask;
			bulletAttack.procCoefficient = procCoefficientPerTick;
			bulletAttack.maxDistance = maxDistance;
			bulletAttack.smartCollision = true;
			bulletAttack.damageType = (Util.CheckRoll(ignitePercentChance, base.characterBody.master) ? DamageType.IgniteOnHit : DamageType.Generic);
			bulletAttack.allowTrajectoryAimAssist = false;
			bulletAttack.Fire();
			if ((bool)base.characterMotor)
			{
				base.characterMotor.ApplyForce(aimRay.direction * (0f - recoilForce));
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= entryDuration && !hasBegunFlamethrower)
		{
			hasBegunFlamethrower = true;
			Util.PlaySound(startAttackSoundString, base.gameObject);
			PlayAnimation("Gesture, Additive", FlamethrowerStateHash, FlamethrowerParamHash, flamethrowerDuration);
			if ((bool)childLocator)
			{
				Transform transform = childLocator.FindChild("MuzzleLeft");
				Transform transform2 = childLocator.FindChild("MuzzleRight");
				if ((bool)transform)
				{
					leftFlamethrowerTransform = Object.Instantiate(flamethrowerEffectPrefab, transform).transform;
				}
				if ((bool)transform2)
				{
					rightFlamethrowerTransform = Object.Instantiate(flamethrowerEffectPrefab, transform2).transform;
				}
				if ((bool)leftFlamethrowerTransform)
				{
					leftFlamethrowerTransform.GetComponent<ScaleParticleSystemDuration>().newDuration = flamethrowerDuration;
				}
				if ((bool)rightFlamethrowerTransform)
				{
					rightFlamethrowerTransform.GetComponent<ScaleParticleSystemDuration>().newDuration = flamethrowerDuration;
				}
			}
			FireGauntlet("MuzzleCenter");
		}
		if (hasBegunFlamethrower)
		{
			flamethrowerStopwatch += Time.deltaTime;
			float num = 1f / tickFrequency / attackSpeedStat;
			if (flamethrowerStopwatch > num)
			{
				flamethrowerStopwatch -= num;
				FireGauntlet("MuzzleCenter");
			}
			UpdateFlamethrowerEffect();
		}
		if (stopwatch >= flamethrowerDuration + entryDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void UpdateFlamethrowerEffect()
	{
		Ray aimRay = GetAimRay();
		Vector3 direction = aimRay.direction;
		Vector3 direction2 = aimRay.direction;
		if ((bool)leftFlamethrowerTransform)
		{
			leftFlamethrowerTransform.forward = direction;
		}
		if ((bool)rightFlamethrowerTransform)
		{
			rightFlamethrowerTransform.forward = direction2;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
