using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Pounder;

public class Pound : BaseState
{
	public static float blastRadius;

	public static float blastProcCoefficient;

	public static float blastForce;

	public static float blastFrequency;

	public static float duration;

	public static GameObject blastEffectPrefab;

	private ProjectileDamage projectileDamage;

	private float poundTimer;

	private static int PoundStateHash = Animator.StringToHash("Pound");

	public override void OnEnter()
	{
		base.OnEnter();
		projectileDamage = GetComponent<ProjectileDamage>();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		poundTimer -= GetDeltaTime();
		if (poundTimer <= 0f && (bool)base.projectileController.owner)
		{
			poundTimer += 1f / blastFrequency;
			if (NetworkServer.active)
			{
				BlastAttack blastAttack = new BlastAttack();
				blastAttack.attacker = base.projectileController.owner;
				blastAttack.baseDamage = projectileDamage.damage;
				blastAttack.baseForce = blastForce;
				blastAttack.crit = projectileDamage.crit;
				blastAttack.damageType = projectileDamage.damageType;
				blastAttack.falloffModel = BlastAttack.FalloffModel.None;
				blastAttack.position = base.transform.position;
				blastAttack.radius = blastRadius;
				blastAttack.teamIndex = base.projectileController.teamFilter.teamIndex;
				blastAttack.Fire();
				EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
				{
					origin = base.transform.position,
					scale = blastRadius
				}, transmit: true);
			}
			PlayAnimation("Base", PoundStateHash);
		}
		if (NetworkServer.active && base.fixedAge > duration)
		{
			EntityState.Destroy(base.gameObject);
		}
	}
}
