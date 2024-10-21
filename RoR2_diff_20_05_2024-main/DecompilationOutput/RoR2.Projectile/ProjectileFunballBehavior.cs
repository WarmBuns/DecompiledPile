using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[Obsolete("This component is deprecated and will likely be removed from future releases.", false)]
[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(TeamFilter))]
public class ProjectileFunballBehavior : NetworkBehaviour
{
	[Tooltip("The effect to use for the explosion.")]
	public GameObject explosionPrefab;

	[Tooltip("How many seconds until detonation.")]
	public float duration;

	[Tooltip("Radius of blast in meters.")]
	public float blastRadius = 1f;

	[Tooltip("Maximum damage of blast.")]
	public float blastDamage = 1f;

	[Tooltip("Force of blast.")]
	public float blastForce = 1f;

	private ProjectileController projectileController;

	[SyncVar]
	private float timer;

	private bool fuseStarted;

	public float Networktimer
	{
		get
		{
			return timer;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref timer, 1u);
		}
	}

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
	}

	private void Start()
	{
		Networktimer = -1f;
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active && fuseStarted)
		{
			Networktimer = timer + Time.fixedDeltaTime;
			if (timer >= duration)
			{
				EffectManager.SpawnEffect(explosionPrefab, new EffectData
				{
					origin = base.transform.position,
					scale = blastRadius
				}, transmit: true);
				BlastAttack blastAttack = new BlastAttack();
				blastAttack.attacker = projectileController.owner;
				blastAttack.inflictor = base.gameObject;
				blastAttack.teamIndex = projectileController.teamFilter.teamIndex;
				blastAttack.position = base.transform.position;
				blastAttack.procChainMask = projectileController.procChainMask;
				blastAttack.procCoefficient = projectileController.procCoefficient;
				blastAttack.radius = blastRadius;
				blastAttack.baseDamage = blastDamage;
				blastAttack.baseForce = blastForce;
				blastAttack.bonusForce = Vector3.zero;
				blastAttack.crit = false;
				blastAttack.damageType = DamageType.Generic;
				blastAttack.Fire();
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		fuseStarted = true;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(timer);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(timer);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			timer = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			timer = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}
