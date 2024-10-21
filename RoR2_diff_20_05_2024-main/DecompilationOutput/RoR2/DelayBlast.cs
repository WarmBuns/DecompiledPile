using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(TeamFilter))]
public class DelayBlast : MonoBehaviour
{
	[HideInInspector]
	public Vector3 position;

	[HideInInspector]
	public GameObject attacker;

	[HideInInspector]
	public GameObject inflictor;

	[HideInInspector]
	public float baseDamage;

	[HideInInspector]
	public bool crit;

	[HideInInspector]
	public float baseForce;

	[HideInInspector]
	public float radius;

	[HideInInspector]
	public Vector3 bonusForce;

	[HideInInspector]
	public float maxTimer;

	[HideInInspector]
	public DamageColorIndex damageColorIndex;

	[HideInInspector]
	public BlastAttack.FalloffModel falloffModel;

	[HideInInspector]
	public DamageTypeCombo damageType = DamageType.Generic;

	[HideInInspector]
	public float procCoefficient = 1f;

	public GameObject explosionEffect;

	public GameObject delayEffect;

	public float timerStagger;

	private float timer;

	private bool hasSpawnedDelayEffect;

	private TeamFilter teamFilter;

	private void Awake()
	{
		teamFilter = GetComponent<TeamFilter>();
		if (!NetworkServer.active)
		{
			base.enabled = false;
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			timer += Time.fixedDeltaTime;
			if ((bool)delayEffect && !hasSpawnedDelayEffect && timer > timerStagger)
			{
				hasSpawnedDelayEffect = true;
				EffectManager.SpawnEffect(delayEffect, new EffectData
				{
					origin = base.transform.position,
					rotation = Util.QuaternionSafeLookRotation(base.transform.forward),
					scale = radius
				}, transmit: true);
			}
			if (timer >= maxTimer + timerStagger)
			{
				Detonate();
			}
		}
	}

	public void Detonate()
	{
		EffectManager.SpawnEffect(explosionEffect, new EffectData
		{
			origin = base.transform.position,
			rotation = Util.QuaternionSafeLookRotation(base.transform.forward),
			scale = radius
		}, transmit: true);
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.position = position;
		blastAttack.baseDamage = baseDamage;
		blastAttack.baseForce = baseForce;
		blastAttack.bonusForce = bonusForce;
		blastAttack.radius = radius;
		blastAttack.attacker = attacker;
		blastAttack.inflictor = inflictor;
		blastAttack.teamIndex = teamFilter.teamIndex;
		blastAttack.crit = crit;
		blastAttack.damageColorIndex = damageColorIndex;
		blastAttack.damageType = damageType;
		blastAttack.falloffModel = falloffModel;
		blastAttack.procCoefficient = procCoefficient;
		blastAttack.Fire();
		Object.Destroy(base.gameObject);
	}
}
