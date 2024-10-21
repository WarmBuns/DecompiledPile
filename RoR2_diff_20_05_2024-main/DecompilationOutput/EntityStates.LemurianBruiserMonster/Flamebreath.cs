using RoR2;
using UnityEngine;

namespace EntityStates.LemurianBruiserMonster;

public class Flamebreath : BaseState
{
	public static GameObject flamethrowerEffectPrefab;

	public static GameObject impactEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float maxDistance;

	public static float radius;

	public static float baseEntryDuration = 1f;

	public static float baseExitDuration = 0.5f;

	public static float baseFlamethrowerDuration = 2f;

	public static float totalDamageCoefficient = 1.2f;

	public static float procCoefficientPerTick;

	public static float tickFrequency;

	public static float force = 20f;

	public static string startAttackSoundString;

	public static string endAttackSoundString;

	public static float ignitePercentChance;

	public static float maxSpread;

	private float tickDamageCoefficient;

	private float flamethrowerStopwatch;

	private float stopwatch;

	private float entryDuration;

	private float exitDuration;

	private float flamethrowerDuration;

	private bool hasBegunFlamethrower;

	private ChildLocator childLocator;

	private Transform flamethrowerEffectInstance;

	private Transform muzzleTransform;

	private bool isCrit;

	private BulletAttack bulletAttack;

	private EffectManagerHelper _emh_flamethrowerEffectInstance;

	private static int PrepFlamebreathStateHash = Animator.StringToHash("PrepFlamebreath");

	private static int PrepFlamebreathParamHash = Animator.StringToHash("PrepFlamebreath.playbackRate");

	private const float flamethrowerEffectBaseDistance = 16f;

	private static int FlamebreathStateHash = Animator.StringToHash("Flamebreath");

	private static int FlamebreathParamHash = Animator.StringToHash("Flamebreath.playbackRate");

	public override void Reset()
	{
		base.Reset();
		tickDamageCoefficient = 0f;
		flamethrowerStopwatch = 0f;
		stopwatch = 0f;
		entryDuration = 0f;
		exitDuration = 0f;
		flamethrowerDuration = 0f;
		hasBegunFlamethrower = false;
		childLocator = null;
		flamethrowerEffectInstance = null;
		muzzleTransform = null;
		isCrit = false;
		if (bulletAttack != null)
		{
			bulletAttack.Reset();
		}
		_emh_flamethrowerEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		entryDuration = baseEntryDuration / attackSpeedStat;
		exitDuration = baseExitDuration / attackSpeedStat;
		flamethrowerDuration = baseFlamethrowerDuration;
		Transform modelTransform = GetModelTransform();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(entryDuration + flamethrowerDuration + 1f);
		}
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			modelTransform.GetComponent<AimAnimator>().enabled = true;
		}
		float num = flamethrowerDuration * tickFrequency;
		tickDamageCoefficient = totalDamageCoefficient / num;
		if (base.isAuthority && (bool)base.characterBody)
		{
			isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		}
		PlayAnimation("Gesture, Override", PrepFlamebreathStateHash, PrepFlamebreathParamHash, entryDuration);
	}

	protected void DestroyFlameThrowerEffect()
	{
		if (flamethrowerEffectInstance != null)
		{
			if (_emh_flamethrowerEffectInstance != null && _emh_flamethrowerEffectInstance.OwningPool != null)
			{
				_emh_flamethrowerEffectInstance.OwningPool.ReturnObject(_emh_flamethrowerEffectInstance);
			}
			else
			{
				EntityState.Destroy(flamethrowerEffectInstance.gameObject);
			}
			flamethrowerEffectInstance = null;
			_emh_flamethrowerEffectInstance = null;
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(endAttackSoundString, base.gameObject);
		PlayCrossfade("Gesture, Override", "BufferEmpty", 0.1f);
		DestroyFlameThrowerEffect();
		base.OnExit();
	}

	private void FireFlame(string muzzleString)
	{
		GetAimRay();
		if (base.isAuthority && (bool)muzzleTransform)
		{
			if (bulletAttack == null)
			{
				bulletAttack = new BulletAttack();
			}
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = muzzleTransform.position;
			bulletAttack.aimVector = muzzleTransform.forward;
			bulletAttack.minSpread = 0f;
			bulletAttack.maxSpread = maxSpread;
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
			bulletAttack.Fire();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= entryDuration && stopwatch < entryDuration + flamethrowerDuration && !hasBegunFlamethrower)
		{
			hasBegunFlamethrower = true;
			Util.PlaySound(startAttackSoundString, base.gameObject);
			PlayAnimation("Gesture, Override", FlamebreathStateHash, FlamebreathParamHash, flamethrowerDuration);
			if ((bool)childLocator)
			{
				muzzleTransform = childLocator.FindChild("MuzzleMouth");
				if (!EffectManager.ShouldUsePooledEffect(flamethrowerEffectPrefab))
				{
					flamethrowerEffectInstance = Object.Instantiate(flamethrowerEffectPrefab, muzzleTransform).transform;
				}
				else
				{
					_emh_flamethrowerEffectInstance = EffectManager.GetAndActivatePooledEffect(flamethrowerEffectPrefab, muzzleTransform, inResetLocal: true);
					flamethrowerEffectInstance = _emh_flamethrowerEffectInstance.gameObject.transform;
				}
				flamethrowerEffectInstance.transform.localPosition = Vector3.zero;
				flamethrowerEffectInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = flamethrowerDuration;
			}
		}
		if (stopwatch >= entryDuration + flamethrowerDuration && hasBegunFlamethrower)
		{
			hasBegunFlamethrower = false;
			PlayCrossfade("Gesture, Override", "ExitFlamebreath", "ExitFlamebreath.playbackRate", exitDuration, 0.1f);
		}
		if (hasBegunFlamethrower)
		{
			flamethrowerStopwatch += Time.deltaTime;
			if (flamethrowerStopwatch > 1f / tickFrequency)
			{
				flamethrowerStopwatch -= 1f / tickFrequency;
				FireFlame("MuzzleCenter");
			}
		}
		else
		{
			DestroyFlameThrowerEffect();
		}
		if (stopwatch >= flamethrowerDuration + entryDuration + exitDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
