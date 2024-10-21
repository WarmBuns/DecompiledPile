using RoR2;
using UnityEngine;

namespace EntityStates.Mage.Weapon;

public class IceNova : BaseState
{
	public static GameObject impactEffectPrefab;

	public static GameObject novaEffectPrefab;

	public static float baseStartDuration;

	public static float baseEndDuration = 2f;

	public static float damageCoefficient = 1.2f;

	public static float procCoefficient;

	public static float force = 20f;

	public static float novaRadius;

	public static string attackString;

	private float stopwatch;

	private float startDuration;

	private float endDuration;

	private bool hasCastNova;

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		endDuration = baseEndDuration / attackSpeedStat;
		startDuration = baseStartDuration / attackSpeedStat;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= startDuration && !hasCastNova)
		{
			hasCastNova = true;
			EffectManager.SpawnEffect(novaEffectPrefab, new EffectData
			{
				origin = base.transform.position,
				scale = novaRadius
			}, transmit: true);
			BlastAttack obj = new BlastAttack
			{
				radius = novaRadius,
				procCoefficient = procCoefficient,
				position = base.transform.position,
				attacker = base.gameObject,
				crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master),
				baseDamage = base.characterBody.damage * damageCoefficient,
				falloffModel = BlastAttack.FalloffModel.None,
				damageType = DamageType.Freeze2s,
				baseForce = force
			};
			obj.teamIndex = TeamComponent.GetObjectTeam(obj.attacker);
			obj.Fire();
		}
		if (stopwatch >= startDuration + endDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
