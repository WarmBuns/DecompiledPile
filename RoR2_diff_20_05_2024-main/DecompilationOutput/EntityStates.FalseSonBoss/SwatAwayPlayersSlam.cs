using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class SwatAwayPlayersSlam : BaseCharacterMain
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public string hitBoxGroupName;

	[SerializeField]
	public GameObject hitEffectPrefab;

	[SerializeField]
	public float procCoefficient;

	[SerializeField]
	public float pushAwayForce;

	[SerializeField]
	public Vector3 forceVector;

	[SerializeField]
	public NetworkSoundEventDef impactSound;

	public float charge;

	public static float minimumDuration;

	public static float blastRadius;

	public static float blastProcCoefficient;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static string enterSoundString;

	public static Vector3 blastBonusForce;

	public static GameObject blastImpactEffectPrefab;

	public static GameObject blastEffectPrefab;

	public static GameObject fistEffectPrefab;

	public static GameObject swingEffectPrefab;

	public static GameObject fissureSlamObject;

	private GameObject swingEffectInstance;

	private bool detonateNextFrame;

	private bool slamComplete;

	protected HitBoxGroup hitBoxGroup;

	private OverlapAttack overlapAttack;

	public override void OnEnter()
	{
		base.OnEnter();
		baseDuration /= attackSpeedStat;
		PlayAnimation("FullBody, Override", "ChargeSwing", "ChargeSwing.playbackRate", baseDuration);
		Util.PlaySound(enterSoundString, base.gameObject);
		swingEffectInstance = Object.Instantiate(swingEffectPrefab, FindModelChild("OverHeadSwingPoint"));
		if (base.isAuthority)
		{
			hitBoxGroup = FindHitBoxGroup(GetHitBoxGroupName());
			if ((bool)hitBoxGroup)
			{
				overlapAttack = new OverlapAttack
				{
					attacker = base.gameObject,
					damage = damageCoefficient * damageStat,
					damageColorIndex = DamageColorIndex.Default,
					damageType = DamageType.Generic,
					forceVector = forceVector,
					hitBoxGroup = hitBoxGroup,
					hitEffectPrefab = hitEffectPrefab,
					impactSound = (impactSound?.index ?? NetworkSoundEventIndex.Invalid),
					inflictor = base.gameObject,
					isCrit = RollCrit(),
					procChainMask = default(ProcChainMask),
					pushAwayForce = pushAwayForce,
					procCoefficient = procCoefficient,
					teamIndex = GetTeam()
				};
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!slamComplete && base.fixedAge >= baseDuration - baseDuration * 0.8f)
		{
			BeginMeleeAttack();
			slamComplete = true;
		}
		if (slamComplete)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	protected void BeginMeleeAttack()
	{
		Vector3 position = FindModelChild("ClubExplosionPoint").transform.position;
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = position,
			scale = blastRadius
		}, transmit: true);
		if (base.isAuthority && overlapAttack != null)
		{
			overlapAttack.Fire();
		}
	}

	public virtual string GetHitBoxGroupName()
	{
		return hitBoxGroupName;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
