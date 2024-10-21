using RoR2;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class OverloadSpike : BaseState
{
	[SerializeField]
	public float duration;

	public static GameObject rainCrashEffectPrefab;

	public static GameObject lunarRainPrefab;

	public static GameObject blastImpactEffectPrefab;

	public static GameObject blastEffectPrefab;

	public static float blastRadius;

	public static float blastProcCoefficient;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static Vector3 blastBonusForce;

	private bool firedSpike;

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.characterBody.GetComponent<FalseSonBossController>().firedLunarSpike)
		{
			PlayAnimation("FullBody, Override", "OverloadErruption", "OverloadErruption.playbackRate", duration);
		}
		else
		{
			outer.SetNextStateToMain();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration - duration * 0.3f && !firedSpike && !base.characterBody.GetComponent<FalseSonBossController>().firedLunarSpike)
		{
			DetonateAuthority();
			base.characterBody.GetComponent<FalseSonBossController>().SpawnLunarSpike();
			firedSpike = true;
		}
		if (base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		outer.SetNextStateToMain();
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = base.gameObject.transform.position,
			scale = blastRadius
		}, transmit: true);
		return new BlastAttack
		{
			attacker = base.gameObject,
			baseDamage = damageStat * blastDamageCoefficient,
			baseForce = blastForce,
			bonusForce = blastBonusForce,
			crit = RollCrit(),
			falloffModel = BlastAttack.FalloffModel.None,
			procCoefficient = blastProcCoefficient,
			radius = blastRadius,
			position = base.gameObject.transform.position,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab),
			teamIndex = base.teamComponent.teamIndex
		}.Fire();
	}
}
