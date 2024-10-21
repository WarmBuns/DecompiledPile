using RoR2;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class CorruptedPaths : BaseCharacterMain
{
	[SerializeField]
	public float baseDuration;

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

	private GameObject leftFistEffectInstance;

	private GameObject rightFistEffectInstance;

	private GameObject swingEffectInstance;

	private bool detonateNextFrame;

	public override void OnEnter()
	{
		base.OnEnter();
		baseDuration /= attackSpeedStat;
		PlayAnimation("FullBody, Override", "ChargeSwing", "ChargeSwing.playbackRate", baseDuration);
		Util.PlaySound(enterSoundString, base.gameObject);
		swingEffectInstance = Object.Instantiate(swingEffectPrefab, FindModelChild("OverHeadSwingPoint"));
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= minimumDuration)
		{
			DetonateAuthority();
			if (base.characterBody.GetBuffCount(DLC2Content.Buffs.CorruptionFesters) == 0)
			{
				outer.SetNextState(new CorruptedPathsDash());
			}
			else
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		Vector3 position = FindModelChild("ClubExplosionPoint").transform.position;
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = position,
			scale = blastRadius
		}, transmit: true);
		return new BlastAttack
		{
			attacker = base.gameObject,
			baseDamage = damageStat * (blastDamageCoefficient * charge) + (base.characterBody.maxHealth - (base.characterBody.baseMaxHealth + base.characterBody.levelMaxHealth * (float)((int)base.characterBody.level - 1))) * 0.01f,
			baseForce = blastForce,
			bonusForce = blastBonusForce,
			crit = RollCrit(),
			falloffModel = BlastAttack.FalloffModel.None,
			procCoefficient = blastProcCoefficient,
			radius = blastRadius,
			position = position,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab),
			teamIndex = base.teamComponent.teamIndex
		}.Fire();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
