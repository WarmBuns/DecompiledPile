using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSon;

public class LaserFatherCharged : BaseState
{
	[SerializeField]
	public GameObject effectPrefab;

	[SerializeField]
	public GameObject hitEffectPrefab;

	[SerializeField]
	public GameObject laserPrefab;

	public static string playAttackSoundString;

	public static string playLoopSoundString;

	public static string stopLoopSoundString;

	public static float damageCoefficient;

	public static float force;

	public static float minSpread;

	public static float maxSpread;

	public static int bulletCount;

	public static float fireFrequency;

	public static float maxDistance;

	public static float minimumDuration;

	public static float maximumDuration;

	public static float lockOnAngle;

	public static float procCoefficientPerTick;

	private HurtBox lockedOnHurtBox;

	private float fireStopwatch;

	private float stopwatch;

	private float refillSecondaryStopwatch;

	private float maxSecondaryStock;

	private float secondaryStock;

	private Ray aimRay;

	private Transform modelTransform;

	private GameObject laserEffect;

	private ChildLocator laserChildLocator;

	private Transform laserEffectEnd;

	public float charge;

	protected Transform muzzleTransform;

	private float bonusDuration;

	protected CameraTargetParams.AimRequest aimRequest;

	public static int abilityAimType;

	private float duration;

	public float walkSpeedCoefficient = 1f;

	private int lunarSpikeCount;

	public static event Action<float> laserTracking;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = maximumDuration;
		aimRequest = base.cameraTargetParams.RequestAimType((CameraTargetParams.AimType)abilityAimType);
		base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedCoefficient;
		FalseSonController component = GetComponent<FalseSonController>();
		if ((bool)component)
		{
			lunarSpikeCount = component.GetTotalSpikeCount();
		}
		else
		{
			lunarSpikeCount = 0;
		}
		Util.PlaySound("Play_falseson_skill4_laser_start", base.gameObject);
		PlayCrossfade("Gesture, Head, Override", "FireLaserLoop", 0.25f);
		PlayCrossfade("Gesture, Head, Additive", "FireLaserLoop", 0.25f);
		aimRay = GetAimRay();
		TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, maxDistance, base.gameObject);
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component2 = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component2)
			{
				muzzleTransform = component2.FindChild("MuzzleLaser");
				if ((bool)muzzleTransform && (bool)laserPrefab)
				{
					laserEffect = UnityEngine.Object.Instantiate(laserPrefab, muzzleTransform.position, muzzleTransform.rotation);
					laserEffect.transform.parent = muzzleTransform;
					laserChildLocator = laserEffect.GetComponent<ChildLocator>();
					laserEffectEnd = laserChildLocator.FindChild("LaserEnd");
				}
			}
		}
		bonusDuration = 0f;
		for (int i = 1; i <= lunarSpikeCount; i++)
		{
			bonusDuration += 1f / (0.5f * (float)i + 1f);
		}
		if (bonusDuration < 0f)
		{
			bonusDuration = 0f;
		}
		duration += bonusDuration;
		base.characterBody.SetAimTimer(duration);
		float num = base.characterBody.attackSpeed - base.characterBody.baseAttackSpeed;
		fireFrequency += num * 0.3f;
		maxSecondaryStock = base.skillLocator.GetSkill(SkillSlot.Secondary).maxStock;
		secondaryStock = base.skillLocator.GetSkill(SkillSlot.Secondary).maxStock;
		LaserFatherCharged.laserTracking?.Invoke(duration);
	}

	public override void OnExit()
	{
		aimRequest?.Dispose();
		if ((bool)laserEffect)
		{
			LaserFatherCharged.laserTracking?.Invoke(0f);
			EntityState.Destroy(laserEffect);
		}
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		base.characterBody.SetAimTimer(2f);
		Util.PlaySound("Stop_falseson_skill4_laser_loop", base.gameObject);
		Util.PlaySound("Stop_falseson_skill4_laser_charge_loop", base.gameObject);
		Util.PlaySound("Play_falseson_skill4_laser_end", base.gameObject);
		PlayAnimation("Gesture, Head, Override", "FireLaserLoopEnd");
		PlayAnimation("Gesture, Head, Additive", "FireLaserLoopEnd");
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		fireStopwatch += deltaTime;
		stopwatch += deltaTime;
		refillSecondaryStopwatch += deltaTime;
		if (refillSecondaryStopwatch > 1f)
		{
			maxSecondaryStock = base.skillLocator.GetSkill(SkillSlot.Secondary).maxStock;
			secondaryStock = base.skillLocator.GetSkill(SkillSlot.Secondary).stock;
			if (secondaryStock < maxSecondaryStock)
			{
				base.skillLocator.GetSkill(SkillSlot.Secondary).stock++;
				refillSecondaryStopwatch = 0f;
			}
		}
		aimRay = GetAimRay();
		Vector3 vector = aimRay.origin;
		if ((bool)muzzleTransform)
		{
			vector = muzzleTransform.position;
		}
		Vector3 vector2;
		if ((bool)lockedOnHurtBox)
		{
			vector2 = lockedOnHurtBox.transform.position;
		}
		else
		{
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, maxDistance, base.gameObject);
			vector2 = ((!Util.CharacterRaycast(base.gameObject, aimRay, out var hitInfo, maxDistance, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore)) ? aimRay.GetPoint(maxDistance) : hitInfo.point);
		}
		Ray ray = new Ray(vector, vector2 - vector);
		bool flag = false;
		if ((bool)laserEffect && (bool)laserChildLocator)
		{
			if (Util.CharacterRaycast(base.gameObject, ray, out var hitInfo2, (vector2 - vector).magnitude, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
			{
				vector2 = hitInfo2.point;
				if (Util.CharacterRaycast(base.gameObject, new Ray(vector2 - ray.direction * 0.1f, -ray.direction), out var _, hitInfo2.distance, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
				{
					vector2 = ray.GetPoint(0.1f);
					flag = true;
				}
			}
			laserEffect.transform.rotation = Util.QuaternionSafeLookRotation(vector2 - vector);
			laserEffectEnd.transform.position = vector2;
		}
		if (fireStopwatch > 1f / fireFrequency)
		{
			string targetMuzzle = "MuzzleLaser";
			if (!flag)
			{
				FireBullet(modelTransform, ray, targetMuzzle, (vector2 - ray.origin).magnitude + 0.1f);
			}
			fireStopwatch -= 1f / fireFrequency;
		}
		if (base.isAuthority && stopwatch > duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	private void FireBullet(Transform modelTransform, Ray aimRay, string targetMuzzle, float maxDistance)
	{
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: false);
		}
		if (base.isAuthority)
		{
			if (charge > 0.85f)
			{
				charge = 1f;
			}
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.minSpread = minSpread;
			bulletAttack.maxSpread = maxSpread;
			bulletAttack.bulletCount = 1u;
			bulletAttack.damage = damageCoefficient * damageStat * charge;
			bulletAttack.force = force;
			bulletAttack.muzzleName = targetMuzzle;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
			bulletAttack.procCoefficient = procCoefficientPerTick;
			bulletAttack.HitEffectNormal = false;
			bulletAttack.radius = 1f;
			bulletAttack.maxDistance = maxDistance;
			bulletAttack.Fire();
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(HurtBoxReference.FromHurtBox(lockedOnHurtBox));
		writer.Write(stopwatch);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		HurtBoxReference hurtBoxReference = reader.ReadHurtBoxReference();
		stopwatch = reader.ReadSingle();
		lockedOnHurtBox = hurtBoxReference.ResolveGameObject()?.GetComponent<HurtBox>();
	}
}
