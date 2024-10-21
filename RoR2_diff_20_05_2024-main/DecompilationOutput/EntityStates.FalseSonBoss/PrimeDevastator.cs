using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class PrimeDevastator : BaseState
{
	[SerializeField]
	private float duration = 4f;

	[SerializeField]
	private float damageCoefficient = 2f;

	public static float slamEffectDelay;

	public static float playerLightningDelay;

	public static GameObject projectilePrefab;

	private Transform modelTransform;

	protected Transform muzzleTransform;

	public static GameObject blastImpactEffectPrefab;

	public static GameObject blastEffectPrefab;

	public static GameObject disablingLightningPrefab;

	public static GameObject lightningEffectPrefab;

	public static float blastRadius;

	public static float blastProcCoefficient;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static Vector3 blastBonusForce;

	private Ray projectileRay;

	public static float arcAngle = 7f;

	public static float spreadBloomValue = 0.3f;

	public static GameObject chargeEffectPrefab;

	private bool chargeEffectSpawned;

	private bool slamEffectSpawned;

	private bool playerLightningSpawned;

	private bool orbsSpawned;

	private float orbSpawnTimer;

	private int orbNumber;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("FullBody, Override", "PrimeDevastator", "PrimeDevastator.playbackRate", duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!chargeEffectSpawned && base.fixedAge > duration * 0.3f)
		{
			chargeEffectSpawned = true;
			EffectManager.SpawnEffect(chargeEffectPrefab, new EffectData
			{
				origin = base.gameObject.transform.position,
				scale = 10f
			}, transmit: true);
		}
		if (!slamEffectSpawned && base.fixedAge > slamEffectDelay)
		{
			slamEffectSpawned = true;
			DetonateAuthority();
		}
		if (!playerLightningSpawned && base.isAuthority && base.fixedAge > playerLightningDelay)
		{
			playerLightningSpawned = true;
			if ((bool)disablingLightningPrefab)
			{
				foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
				{
					if (!(instance == null))
					{
						CharacterBody body = instance.master.GetBody();
						if (!(body == null))
						{
							body.AddTimedBuff(DLC2Content.Buffs.DisableAllSkills, 420f);
							EffectManager.SpawnEffect(lightningEffectPrefab, new EffectData
							{
								origin = body.corePosition,
								scale = blastRadius
							}, transmit: true);
						}
					}
				}
			}
		}
		if (!orbsSpawned && base.isAuthority && base.fixedAge > playerLightningDelay)
		{
			orbSpawnTimer += Time.deltaTime;
			if (orbSpawnTimer > 0.5f)
			{
				FireDevastator("MuzzleOrb1");
				orbNumber++;
				orbSpawnTimer = 0f;
			}
			if (orbNumber >= 4)
			{
				orbsSpawned = true;
			}
		}
		if (base.fixedAge >= duration + 1f && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireDevastator(string targetMuzzle)
	{
		projectileRay = GetAimRay();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild(targetMuzzle);
				if ((bool)transform)
				{
					projectileRay.origin = transform.position;
				}
			}
		}
		if (base.isAuthority)
		{
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.position = base.gameObject.transform.position + new Vector3(7f, 0f, 0f);
			fireProjectileInfo.rotation = Quaternion.identity;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void OnExit()
	{
		base.OnExit();
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

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
