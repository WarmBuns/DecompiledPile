using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LaserTurbine;

public class FireMainBeamState : LaserTurbineBaseState
{
	public static float baseDuration;

	public static float mainBeamDamageCoefficient;

	public static float mainBeamProcCoefficient;

	public static float mainBeamForce;

	public static float mainBeamRadius;

	public static float mainBeamMaxDistance;

	public static GameObject forwardBeamTracerEffect;

	public static GameObject backwardBeamTracerEffect;

	public static GameObject mainBeamImpactEffect;

	public static GameObject secondBombPrefab;

	public static float secondBombDamageCoefficient;

	private Ray initialAimRay;

	private Vector3 beamHitPosition;

	private bool isCrit;

	protected override bool shouldFollow => true;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			initialAimRay = GetAimRay();
		}
		if (NetworkServer.active)
		{
			isCrit = base.ownerBody.RollCrit();
			FireBeamServer(initialAimRay, forwardBeamTracerEffect, mainBeamMaxDistance, isInitialBeam: true);
		}
		base.laserTurbineController.showTurbineDisplay = false;
	}

	public override void Update()
	{
		base.Update();
		if (base.isAuthority && base.fixedAge >= baseDuration)
		{
			outer.SetNextState(new RechargeState());
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active && !outer.destroying)
		{
			Vector3 direction = initialAimRay.origin - beamHitPosition;
			Ray aimRay = new Ray(beamHitPosition, direction);
			FireBeamServer(aimRay, backwardBeamTracerEffect, direction.magnitude, isInitialBeam: false);
		}
		base.laserTurbineController.showTurbineDisplay = true;
		base.OnExit();
	}

	private void FireBeamServer(Ray aimRay, GameObject tracerEffectPrefab, float maxDistance, bool isInitialBeam)
	{
		bool didHit = false;
		BulletAttack obj = new BulletAttack
		{
			origin = aimRay.origin,
			aimVector = aimRay.direction,
			bulletCount = 1u,
			damage = GetDamage() * mainBeamDamageCoefficient,
			damageColorIndex = DamageColorIndex.Item,
			damageType = DamageType.Generic,
			falloffModel = BulletAttack.FalloffModel.None,
			force = mainBeamForce,
			hitEffectPrefab = mainBeamImpactEffect,
			HitEffectNormal = false,
			hitMask = LayerIndex.entityPrecise.mask,
			isCrit = isCrit,
			maxDistance = maxDistance,
			minSpread = 0f,
			maxSpread = 0f,
			muzzleName = "",
			owner = base.ownerBody.gameObject,
			procChainMask = default(ProcChainMask),
			procCoefficient = mainBeamProcCoefficient,
			queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
			radius = mainBeamRadius,
			smartCollision = true,
			sniper = false,
			spreadPitchScale = 1f,
			spreadYawScale = 1f,
			stopperMask = LayerIndex.world.mask,
			tracerEffectPrefab = (isInitialBeam ? tracerEffectPrefab : null),
			weapon = base.gameObject
		};
		TeamIndex teamIndex = base.ownerBody.teamComponent.teamIndex;
		obj.hitCallback = delegate(BulletAttack _bulletAttack, ref BulletAttack.BulletHit info)
		{
			bool flag = BulletAttack.defaultHitCallback(_bulletAttack, ref info);
			if (!isInitialBeam)
			{
				return true;
			}
			if (flag)
			{
				HealthComponent healthComponent = (info.hitHurtBox ? info.hitHurtBox.healthComponent : null);
				if ((bool)healthComponent && healthComponent.alive && info.hitHurtBox.teamIndex != teamIndex)
				{
					flag = false;
				}
			}
			if (!flag)
			{
				didHit = true;
				beamHitPosition = info.point;
			}
			return flag;
		};
		obj.filterCallback = delegate(BulletAttack _bulletAttack, ref BulletAttack.BulletHit info)
		{
			return (!info.entityObject || (object)info.entityObject != _bulletAttack.owner) && BulletAttack.defaultFilterCallback(_bulletAttack, ref info);
		};
		obj.Fire();
		if (!didHit)
		{
			if (Physics.Raycast(aimRay, out var hitInfo, mainBeamMaxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				didHit = true;
				beamHitPosition = hitInfo.point;
			}
			else
			{
				beamHitPosition = aimRay.GetPoint(mainBeamMaxDistance);
			}
		}
		if (didHit && isInitialBeam)
		{
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = secondBombPrefab;
			fireProjectileInfo.owner = base.ownerBody.gameObject;
			fireProjectileInfo.position = beamHitPosition - aimRay.direction * 0.5f;
			fireProjectileInfo.rotation = Quaternion.identity;
			fireProjectileInfo.damage = GetDamage() * secondBombDamageCoefficient;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
			fireProjectileInfo.crit = isCrit;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
		if (!isInitialBeam)
		{
			EffectData effectData = new EffectData
			{
				origin = aimRay.origin,
				start = base.transform.position
			};
			effectData.SetNetworkedObjectReference(base.gameObject);
			EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		Vector3 origin = initialAimRay.origin;
		Vector3 direction = initialAimRay.direction;
		writer.Write(origin);
		writer.Write(direction);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		Vector3 origin = reader.ReadVector3();
		Vector3 direction = reader.ReadVector3();
		initialAimRay = new Ray(origin, direction);
	}
}
