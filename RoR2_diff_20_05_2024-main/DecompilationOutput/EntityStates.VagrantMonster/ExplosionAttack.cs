using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VagrantMonster;

public class ExplosionAttack : BaseState
{
	public static float minRadius;

	public static float maxRadius;

	public static int explosionCount;

	public static float baseDuration;

	public static float damageCoefficient;

	public static float force;

	public static float damageScaling;

	public static GameObject novaEffectPrefab;

	private float explosionTimer;

	private float explosionInterval;

	private int explosionIndex;

	public override void OnEnter()
	{
		base.OnEnter();
		explosionInterval = baseDuration / (float)explosionCount;
		explosionIndex = 0;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		explosionTimer -= GetDeltaTime();
		if (!(explosionTimer <= 0f))
		{
			return;
		}
		if (explosionIndex >= explosionCount)
		{
			if (base.isAuthority)
			{
				outer.SetNextStateToMain();
			}
		}
		else
		{
			explosionTimer += explosionInterval;
			Explode();
			explosionIndex++;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void Explode()
	{
		float t = (float)explosionIndex / (float)(explosionCount - 1);
		float num = Mathf.Lerp(minRadius, maxRadius, t);
		EffectManager.SpawnEffect(novaEffectPrefab, new EffectData
		{
			origin = base.transform.position,
			scale = num
		}, transmit: false);
		if (NetworkServer.active)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.gameObject;
			blastAttack.inflictor = base.gameObject;
			blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			blastAttack.baseDamage = damageStat * damageCoefficient * Mathf.Pow(damageScaling, explosionIndex);
			blastAttack.baseForce = force;
			blastAttack.position = base.transform.position;
			blastAttack.radius = num;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			blastAttack.Fire();
		}
	}
}
