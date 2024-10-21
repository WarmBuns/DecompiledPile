using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Chef;

public class YesChef : BaseState
{
	[SerializeField]
	public float duration = 1f;

	[SerializeField]
	public GameObject playerRespawnEffectPrefab;

	[SerializeField]
	public string startSoundString;

	[SerializeField]
	public int buffStockToAdd = 3;

	[SerializeField]
	public float heatDamageCo = 1f;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject blastEffectPrefab;

	[SerializeField]
	public float blastDamageCoefficient;

	[SerializeField]
	public float blastRadius;

	[SerializeField]
	public float blastProcCoefficient;

	[SerializeField]
	public float blastForce;

	[SerializeField]
	[Tooltip("Set This to the child transform you want the blast effect to parent to")]
	public string blastEffectTransformChild;

	[SerializeField]
	public float exitDelay;

	[SerializeField]
	public SkillDef primarySkillOverrideDef;

	[SerializeField]
	public SkillDef secondarySkillOverrideDef;

	[SerializeField]
	public SkillDef utilitySkillOverrideDef;

	[SerializeField]
	public SkillDef specialSkillOverrideDef;

	private bool alreadyRan;

	private ChildLocator childLocator;

	private ChefController chefController;

	private bool playedYesChefSFX;

	public override void OnEnter()
	{
		base.OnEnter();
		chefController = base.characterBody.GetComponent<ChefController>();
		if ((bool)chefController)
		{
			chefController.SetYesChefHeatState(newYesChefHeatState: true);
		}
		if (base.isAuthority && (bool)chefController)
		{
			chefController.ClearSkillOverrides();
			SkillStateOverrideData skillStateOverrideData = new SkillStateOverrideData(base.characterBody, _duplicateStock: true);
			skillStateOverrideData.simulateRestockForOverridenSkills = false;
			skillStateOverrideData.primarySkillOverride = primarySkillOverrideDef;
			skillStateOverrideData.secondarySkillOverride = secondarySkillOverrideDef;
			skillStateOverrideData.utilitySkillOverride = utilitySkillOverrideDef;
			skillStateOverrideData.specialSkillOverride = specialSkillOverrideDef;
			skillStateOverrideData.OverrideSkills(base.skillLocator);
			EnsureMinStock(ref base.skillLocator.primary);
			EnsureMinStock(ref base.skillLocator.secondary);
			EnsureMinStock(ref base.skillLocator.utility);
			chefController.TransferSkillOverrides(skillStateOverrideData);
		}
		childLocator = GetModelChildLocator();
		PlayAnimation("Gesture, Additive", "FireYesChef", "FireYesChef.playbackRate", duration);
		PlayAnimation("Gesture, Override", "FireYesChef", "FireYesChef.playbackRate", duration);
		EffectData effectData = new EffectData
		{
			origin = base.transform.position,
			scale = blastRadius
		};
		if ((bool)childLocator)
		{
			int num = childLocator.FindChildIndex(blastEffectTransformChild);
			if (num != -1)
			{
				effectData.SetChildLocatorTransformReference(base.gameObject, num);
			}
		}
		EffectManager.SpawnEffect(blastEffectPrefab, effectData, transmit: true);
		chefController.ActivateServoStrainSfxForDuration(3f);
		static void EnsureMinStock(ref GenericSkill skill)
		{
			if (skill.stock < 1)
			{
				skill.stock = 1;
				skill.rechargeStopwatch = 0f;
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void OnExit()
	{
		base.OnExit();
		if (NetworkServer.active)
		{
			if (!base.characterBody.HasBuff(DLC2Content.Buffs.Boosted))
			{
				base.characterBody.AddBuff(DLC2Content.Buffs.Boosted);
			}
			if (!base.characterBody.HasBuff(DLC2Content.Buffs.boostedFireEffect))
			{
				base.characterBody.AddBuff(DLC2Content.Buffs.boostedFireEffect);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > 0.5f && !playedYesChefSFX)
		{
			Util.PlaySound(startSoundString, base.gameObject);
			playedYesChefSFX = true;
		}
		if ((bool)chefController && base.fixedAge > duration && base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, base.gameObject.transform.position, Quaternion.identity, base.gameObject, base.characterBody.damage * heatDamageCo, 0f, Util.CheckRoll(critStat, base.characterBody.master));
			Util.PlaySound("Play_chef_skill4_flame_aoe_on", base.gameObject);
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.characterBody.gameObject;
			blastAttack.baseDamage = blastDamageCoefficient * base.characterBody.damage;
			blastAttack.baseForce = blastForce;
			blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			blastAttack.crit = base.characterBody.RollCrit();
			blastAttack.damageColorIndex = DamageColorIndex.Item;
			blastAttack.inflictor = base.gameObject;
			blastAttack.position = base.characterBody.corePosition;
			blastAttack.procChainMask = default(ProcChainMask);
			blastAttack.procCoefficient = blastProcCoefficient;
			blastAttack.radius = blastRadius;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.teamIndex = base.teamComponent.teamIndex;
			blastAttack.Fire();
			outer.SetNextStateToMain();
		}
	}
}
