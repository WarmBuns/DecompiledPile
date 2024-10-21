using System;
using HG;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileImpactExplosion : ProjectileExplosion, IProjectileImpactBehavior
{
	public enum TransformSpace
	{
		World,
		Local,
		Normal
	}

	private Vector3 impactNormal = Vector3.up;

	[Header("Impact & Lifetime Properties")]
	public GameObject impactEffect;

	[HideInInspector]
	[Obsolete("This sound will not play over the network. Use lifetimeExpiredSound instead.", false)]
	[ShowFieldObsolete]
	[Tooltip("This sound will not play over the network. Use lifetimeExpiredSound instead.")]
	public string lifetimeExpiredSoundString;

	public NetworkSoundEventDef lifetimeExpiredSound;

	public float offsetForLifetimeExpiredSound;

	public bool destroyOnEnemy = true;

	public bool destroyOnWorld;

	public bool impactOnWorld = true;

	public bool timerAfterImpact;

	public float lifetime;

	public float lifetimeAfterImpact;

	public float lifetimeRandomOffset;

	private float stopwatch;

	public uint minimumPhysicsStepsToKeepAliveAfterImpact;

	private float stopwatchAfterImpact;

	private bool hasImpact;

	private bool hasPlayedLifetimeExpiredSound;

	public TransformSpace transformSpace;

	public bool explodeOnLifeTimeExpiration;

	protected override void Awake()
	{
		base.Awake();
		lifetime += UnityEngine.Random.Range(0f, lifetimeRandomOffset);
	}

	protected void FixedUpdate()
	{
		stopwatch += Time.fixedDeltaTime;
		if (!NetworkServer.active && !projectileController.isPrediction)
		{
			return;
		}
		if (explodeOnLifeTimeExpiration && alive && stopwatch >= lifetime)
		{
			explosionEffect = impactEffect ?? explosionEffect;
			Detonate();
		}
		if (timerAfterImpact && hasImpact)
		{
			stopwatchAfterImpact += Time.fixedDeltaTime;
		}
		bool num = stopwatch >= lifetime;
		bool flag = timerAfterImpact && stopwatchAfterImpact > lifetimeAfterImpact;
		bool flag2 = (bool)projectileHealthComponent && !projectileHealthComponent.alive;
		if (num || flag || flag2)
		{
			alive = false;
		}
		if (timerAfterImpact && hasImpact && minimumPhysicsStepsToKeepAliveAfterImpact != 0)
		{
			minimumPhysicsStepsToKeepAliveAfterImpact--;
			alive = true;
		}
		if (alive && !hasPlayedLifetimeExpiredSound)
		{
			bool flag3 = stopwatch > lifetime - offsetForLifetimeExpiredSound;
			if (timerAfterImpact)
			{
				flag3 |= stopwatchAfterImpact > lifetimeAfterImpact - offsetForLifetimeExpiredSound;
			}
			if (flag3)
			{
				hasPlayedLifetimeExpiredSound = true;
				if (NetworkServer.active && (bool)lifetimeExpiredSound)
				{
					PointSoundManager.EmitSoundServer(lifetimeExpiredSound.index, base.transform.position);
				}
			}
		}
		if (!alive)
		{
			explosionEffect = impactEffect ?? explosionEffect;
			Detonate();
		}
	}

	protected override Quaternion GetRandomDirectionForChild()
	{
		Quaternion randomChildRollPitch = GetRandomChildRollPitch();
		return transformSpace switch
		{
			TransformSpace.Local => base.transform.rotation * randomChildRollPitch, 
			TransformSpace.Normal => Quaternion.FromToRotation(Vector3.forward, impactNormal) * randomChildRollPitch, 
			_ => randomChildRollPitch, 
		};
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (!alive)
		{
			return;
		}
		Collider collider = impactInfo.collider;
		impactNormal = impactInfo.estimatedImpactNormal;
		if (!collider)
		{
			return;
		}
		DamageInfo damageInfo = new DamageInfo();
		if ((bool)projectileDamage)
		{
			damageInfo.damage = projectileDamage.damage;
			damageInfo.crit = projectileDamage.crit;
			damageInfo.attacker = (projectileController.owner ? projectileController.owner.gameObject : null);
			damageInfo.inflictor = base.gameObject;
			damageInfo.position = impactInfo.estimatedPointOfImpact;
			damageInfo.force = projectileDamage.force * base.transform.forward;
			damageInfo.procChainMask = projectileController.procChainMask;
			damageInfo.procCoefficient = projectileController.procCoefficient;
		}
		HurtBox component = collider.GetComponent<HurtBox>();
		if ((bool)component)
		{
			if (destroyOnEnemy)
			{
				HealthComponent healthComponent = component.healthComponent;
				if ((bool)healthComponent)
				{
					if (healthComponent.gameObject == projectileController.owner || ((bool)projectileHealthComponent && healthComponent == projectileHealthComponent))
					{
						return;
					}
					alive = false;
				}
			}
		}
		else if (destroyOnWorld)
		{
			alive = false;
		}
		hasImpact = (bool)component || impactOnWorld;
		if (NetworkServer.active && hasImpact)
		{
			GlobalEventManager.instance.OnHitAll(damageInfo, collider.gameObject);
		}
	}

	protected override void OnValidate()
	{
		if (!Application.IsPlaying(this))
		{
			base.OnValidate();
			if (!string.IsNullOrEmpty(lifetimeExpiredSoundString))
			{
				Debug.LogWarningFormat(base.gameObject, "{0} ProjectileImpactExplosion component supplies a value in the lifetimeExpiredSoundString field. This will not play correctly over the network. Please use lifetimeExpiredSound instead.", Util.GetGameObjectHierarchyName(base.gameObject));
			}
		}
	}
}
