using System;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileExplosion : MonoBehaviour
{
	protected ProjectileController projectileController;

	protected ProjectileDamage projectileDamage;

	protected bool alive = true;

	[Header("Main Properties")]
	public BlastAttack.FalloffModel falloffModel = BlastAttack.FalloffModel.Linear;

	public float blastRadius;

	[Tooltip("The percentage of the damage, proc coefficient, and force of the initial projectile. Ranges from 0-1")]
	public float blastDamageCoefficient;

	public float blastProcCoefficient = 1f;

	public AttackerFiltering blastAttackerFiltering;

	public Vector3 bonusBlastForce;

	public bool canRejectForce = true;

	public HealthComponent projectileHealthComponent;

	public GameObject explosionEffect;

	[Obsolete("This sound will not play over the network. Provide the sound via the prefab referenced by explosionEffect instead.", false)]
	[ShowFieldObsolete]
	[Tooltip("This sound will not play over the network. Provide the sound via the prefab referenced by explosionEffect instead.")]
	public string explosionSoundString;

	[Header("Child Properties")]
	[Tooltip("Does this projectile release children on death?")]
	public bool fireChildren;

	public GameObject childrenProjectilePrefab;

	public int childrenCount;

	[Tooltip("What percentage of our damage does the children get?")]
	public float childrenDamageCoefficient;

	[ShowFieldObsolete]
	[Tooltip("How to randomize the orientation of children")]
	public Vector3 minAngleOffset;

	[ShowFieldObsolete]
	public Vector3 maxAngleOffset;

	public float minRollDegrees;

	public float rangeRollDegrees;

	public float minPitchDegrees;

	public float rangePitchDegrees;

	[Tooltip("useLocalSpaceForChildren is unused by ProjectileImpactExplosion")]
	public bool useLocalSpaceForChildren;

	[Header("DoT Properties")]
	[Tooltip("If true, applies a DoT given the following properties")]
	public bool applyDot;

	public DotController.DotIndex dotIndex = DotController.DotIndex.None;

	[Tooltip("Duration in seconds of the DoT.  Unused if calculateTotalDamage is true.")]
	public float dotDuration;

	[Tooltip("Multiplier on the per-tick damage")]
	public float dotDamageMultiplier = 1f;

	[Tooltip("If true, we cap the numer of DoT stacks for this attacker.")]
	public bool applyMaxStacksFromAttacker;

	[Tooltip("The maximum number of stacks that we can apply for this attacker")]
	public uint maxStacksFromAttacker = uint.MaxValue;

	[Tooltip("If true, we disregard the duration and instead specify the total damage.")]
	public bool calculateTotalDamage;

	[Tooltip("totalDamage = totalDamageMultiplier * attacker's damage")]
	public float totalDamageMultiplier;

	protected virtual void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
	}

	public void Detonate()
	{
		if (NetworkServer.active)
		{
			DetonateServer();
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	protected void DetonateServer()
	{
		if ((bool)explosionEffect)
		{
			EffectManager.SpawnEffect(explosionEffect, new EffectData
			{
				origin = base.transform.position,
				scale = blastRadius
			}, transmit: true);
		}
		if ((bool)projectileDamage)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.position = base.transform.position;
			blastAttack.baseDamage = projectileDamage.damage * blastDamageCoefficient;
			blastAttack.baseForce = projectileDamage.force * blastDamageCoefficient;
			blastAttack.radius = blastRadius;
			blastAttack.attacker = (projectileController.owner ? projectileController.owner.gameObject : null);
			blastAttack.inflictor = base.gameObject;
			blastAttack.teamIndex = projectileController.teamFilter.teamIndex;
			blastAttack.crit = projectileDamage.crit;
			blastAttack.procChainMask = projectileController.procChainMask;
			blastAttack.procCoefficient = projectileController.procCoefficient * blastProcCoefficient;
			blastAttack.bonusForce = bonusBlastForce;
			blastAttack.falloffModel = falloffModel;
			blastAttack.damageColorIndex = projectileDamage.damageColorIndex;
			blastAttack.damageType = projectileDamage.damageType;
			blastAttack.attackerFiltering = blastAttackerFiltering;
			blastAttack.canRejectForce = canRejectForce;
			BlastAttack.Result result = blastAttack.Fire();
			OnBlastAttackResult(blastAttack, result);
		}
		if (explosionSoundString.Length > 0)
		{
			Util.PlaySound(explosionSoundString, base.gameObject);
		}
		if (fireChildren)
		{
			for (int i = 0; i < childrenCount; i++)
			{
				FireChild();
			}
		}
	}

	protected Quaternion GetRandomChildRollPitch()
	{
		Quaternion quaternion = Quaternion.AngleAxis(minRollDegrees + UnityEngine.Random.Range(0f, rangeRollDegrees), Vector3.forward);
		Quaternion quaternion2 = Quaternion.AngleAxis(minPitchDegrees + UnityEngine.Random.Range(0f, rangePitchDegrees), Vector3.left);
		return quaternion * quaternion2;
	}

	protected virtual Quaternion GetRandomDirectionForChild()
	{
		Quaternion randomChildRollPitch = GetRandomChildRollPitch();
		if (useLocalSpaceForChildren)
		{
			return base.transform.rotation * randomChildRollPitch;
		}
		return randomChildRollPitch;
	}

	protected void FireChild()
	{
		Quaternion randomDirectionForChild = GetRandomDirectionForChild();
		GameObject obj = UnityEngine.Object.Instantiate(childrenProjectilePrefab, base.transform.position, randomDirectionForChild);
		ProjectileController component = obj.GetComponent<ProjectileController>();
		if ((bool)component)
		{
			component.procChainMask = projectileController.procChainMask;
			component.procCoefficient = projectileController.procCoefficient;
			component.Networkowner = projectileController.owner;
		}
		obj.GetComponent<TeamFilter>().teamIndex = GetComponent<TeamFilter>().teamIndex;
		ProjectileDamage component2 = obj.GetComponent<ProjectileDamage>();
		if ((bool)component2)
		{
			component2.damage = projectileDamage.damage * childrenDamageCoefficient;
			component2.crit = projectileDamage.crit;
			component2.force = projectileDamage.force;
			component2.damageColorIndex = projectileDamage.damageColorIndex;
		}
		NetworkServer.Spawn(obj);
	}

	public void SetExplosionRadius(float newRadius)
	{
		blastRadius = newRadius;
	}

	public void SetAlive(bool newAlive)
	{
		alive = newAlive;
	}

	public bool GetAlive()
	{
		if (!NetworkServer.active)
		{
			return false;
		}
		return alive;
	}

	protected virtual void OnValidate()
	{
		if (!Application.IsPlaying(this) && !string.IsNullOrEmpty(explosionSoundString))
		{
			Debug.LogWarningFormat(base.gameObject, "{0} ProjectileImpactExplosion component supplies a value in the explosionSoundString field. This will not play correctly over the network. Please move the sound to the explosion effect.", Util.GetGameObjectHierarchyName(base.gameObject));
		}
	}

	protected virtual void OnBlastAttackResult(BlastAttack blastAttack, BlastAttack.Result result)
	{
		if (!applyDot)
		{
			return;
		}
		CharacterBody characterBody = blastAttack.attacker?.GetComponent<CharacterBody>();
		BlastAttack.HitPoint[] hitPoints = result.hitPoints;
		for (int i = 0; i < hitPoints.Length; i++)
		{
			BlastAttack.HitPoint hitPoint = hitPoints[i];
			if ((bool)hitPoint.hurtBox && (bool)hitPoint.hurtBox.healthComponent)
			{
				InflictDotInfo inflictDotInfo = default(InflictDotInfo);
				inflictDotInfo.victimObject = hitPoint.hurtBox.healthComponent.gameObject;
				inflictDotInfo.attackerObject = blastAttack.attacker;
				inflictDotInfo.dotIndex = dotIndex;
				inflictDotInfo.damageMultiplier = dotDamageMultiplier;
				InflictDotInfo dotInfo = inflictDotInfo;
				if (calculateTotalDamage && (bool)characterBody)
				{
					dotInfo.totalDamage = characterBody.damage * totalDamageMultiplier;
				}
				else
				{
					dotInfo.duration = dotDuration;
				}
				if (applyMaxStacksFromAttacker)
				{
					dotInfo.maxStacksFromAttacker = maxStacksFromAttacker;
				}
				if ((bool)characterBody && (bool)characterBody.inventory)
				{
					StrengthenBurnUtils.CheckDotForUpgrade(characterBody.inventory, ref dotInfo);
				}
				DotController.InflictDot(ref dotInfo);
			}
		}
	}
}
