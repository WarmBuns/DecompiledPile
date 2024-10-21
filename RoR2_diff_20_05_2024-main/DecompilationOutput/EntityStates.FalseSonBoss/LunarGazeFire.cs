using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSonBoss;

public class LunarGazeFire : BaseState
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

	public static DamageType lunarGazeDamageType;

	public static bool isLunarGazePlusPlus;

	private HurtBox lockedOnHurtBox;

	private float fireStopwatch;

	private float stopwatch;

	private Ray aimRay;

	private Transform modelTransform;

	private GameObject laserEffect;

	private ChildLocator laserChildLocator;

	private Transform laserEffectEnd;

	protected Transform muzzleTransform;

	private BullseyeSearch enemyFinder;

	private bool foundAnyTarget;

	public override void OnEnter()
	{
		base.OnEnter();
		base.characterBody.SetAimTimer(maximumDuration);
		Util.PlaySound(playAttackSoundString, base.gameObject);
		Util.PlaySound(playLoopSoundString, base.gameObject);
		PlayCrossfade("Gesture, Additive", "LaserBlastLoop", 0.25f);
		PlayCrossfade("Gesture, Override", "LaserBlastLoop", 0.25f);
		enemyFinder = new BullseyeSearch();
		enemyFinder.viewer = base.characterBody;
		enemyFinder.maxDistanceFilter = maxDistance;
		enemyFinder.maxAngleFilter = lockOnAngle;
		enemyFinder.searchOrigin = aimRay.origin;
		enemyFinder.searchDirection = aimRay.direction;
		enemyFinder.filterByLoS = false;
		enemyFinder.sortMode = BullseyeSearch.SortMode.Angle;
		enemyFinder.teamMaskFilter = TeamMask.allButNeutral;
		if ((bool)base.teamComponent)
		{
			enemyFinder.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		aimRay = GetAimRay();
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				muzzleTransform = component.FindChild("MuzzleLaser");
				if ((bool)muzzleTransform && (bool)laserPrefab)
				{
					laserEffect = Object.Instantiate(laserPrefab, muzzleTransform.position, muzzleTransform.rotation);
					laserEffect.transform.parent = muzzleTransform;
					laserChildLocator = laserEffect.GetComponent<ChildLocator>();
					laserEffectEnd = laserChildLocator.FindChild("LaserEnd");
				}
			}
		}
		UpdateLockOn();
	}

	public override void OnExit()
	{
		PlayCrossfade("Gesture, Additive", "Empty", 0.25f);
		PlayCrossfade("Gesture, Override", "Empty", 0.25f);
		if ((bool)laserEffect)
		{
			EntityState.Destroy(laserEffect);
		}
		base.characterBody.SetAimTimer(2f);
		Util.PlaySound(stopLoopSoundString, base.gameObject);
		base.OnExit();
	}

	private void UpdateLockOn()
	{
		if (base.isAuthority)
		{
			enemyFinder.searchOrigin = aimRay.origin;
			enemyFinder.searchDirection = aimRay.direction;
			enemyFinder.RefreshCandidates();
			HurtBox hurtBox = enemyFinder.GetResults().FirstOrDefault();
			if (!(lockedOnHurtBox != null) || !(hurtBox == null) || !enemyFinder.CheckVisible(lockedOnHurtBox.healthComponent.gameObject))
			{
				lockedOnHurtBox = hurtBox;
				foundAnyTarget = hurtBox;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireStopwatch += GetDeltaTime();
		stopwatch += GetDeltaTime();
		aimRay = GetAimRay();
		Vector3 vector = aimRay.origin;
		if ((bool)muzzleTransform)
		{
			vector = muzzleTransform.position;
		}
		RaycastHit hitInfo;
		Vector3 vector2 = (lockedOnHurtBox ? lockedOnHurtBox.transform.position : ((!Util.CharacterRaycast(base.gameObject, aimRay, out hitInfo, maxDistance, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore)) ? aimRay.GetPoint(maxDistance) : hitInfo.point));
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
			UpdateLockOn();
			fireStopwatch -= 1f / fireFrequency;
		}
		if (base.isAuthority && (((!base.inputBank || !base.inputBank.skill4.down) && stopwatch > minimumDuration) || stopwatch > maximumDuration))
		{
			Util.PlaySound("Play_boss_falseson_skill3_lunarGaze_end", base.gameObject);
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
		if (!base.isAuthority)
		{
			return;
		}
		if (isLunarGazePlusPlus)
		{
			if (Util.CheckRoll(10f))
			{
				lunarGazeDamageType = DamageType.LunarRuin;
			}
			else
			{
				lunarGazeDamageType = DamageType.Generic;
			}
		}
		BulletAttack bulletAttack = new BulletAttack();
		bulletAttack.owner = base.gameObject;
		bulletAttack.weapon = base.gameObject;
		bulletAttack.origin = aimRay.origin;
		bulletAttack.aimVector = aimRay.direction;
		bulletAttack.minSpread = minSpread;
		bulletAttack.maxSpread = maxSpread;
		bulletAttack.bulletCount = 1u;
		bulletAttack.damage = damageCoefficient * damageStat / fireFrequency;
		bulletAttack.force = force;
		bulletAttack.damageType = lunarGazeDamageType;
		bulletAttack.muzzleName = targetMuzzle;
		bulletAttack.hitEffectPrefab = hitEffectPrefab;
		bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
		bulletAttack.procCoefficient = procCoefficientPerTick;
		bulletAttack.HitEffectNormal = false;
		bulletAttack.radius = 0f;
		bulletAttack.maxDistance = maxDistance;
		bulletAttack.Fire();
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
