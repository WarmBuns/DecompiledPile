using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GhostGunController : MonoBehaviour
{
	public GameObject owner;

	public float interval;

	public float maxRange = 20f;

	public float turnSpeed = 180f;

	public Vector3 localOffset = Vector3.zero;

	public float positionSmoothTime = 0.05f;

	public float timeout = 2f;

	private float fireTimer;

	private float timeoutTimer;

	private int ammo;

	private int kills;

	private GameObject target;

	private Vector3 velocity;

	private void Start()
	{
		fireTimer = 0f;
		ammo = 6;
		kills = 0;
		timeoutTimer = timeout;
	}

	private void Fire(Vector3 origin, Vector3 aimDirection)
	{
		BulletAttack obj = new BulletAttack
		{
			aimVector = aimDirection,
			bulletCount = 1u,
			damage = CalcDamage(),
			force = 2400f,
			maxSpread = 0f,
			minSpread = 0f,
			muzzleName = "muzzle",
			origin = origin,
			owner = owner,
			procCoefficient = 0f,
			tracerEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/TracerSmokeChase"),
			hitEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Hitspark1"),
			damageColorIndex = DamageColorIndex.Item
		};
		GlobalEventManager.onCharacterDeathGlobal += CheckForKill;
		obj.Fire();
		GlobalEventManager.onCharacterDeathGlobal -= CheckForKill;
	}

	private void CheckForKill(DamageReport damageReport)
	{
		if (damageReport.damageInfo.inflictor == base.gameObject)
		{
			kills++;
		}
	}

	private float CalcDamage()
	{
		float damage = owner.GetComponent<CharacterBody>().damage;
		return 5f * Mathf.Pow(2f, kills) * damage;
	}

	private bool HasLoS(GameObject target)
	{
		Ray ray = new Ray(base.transform.position, target.transform.position - base.transform.position);
		RaycastHit hitInfo = default(RaycastHit);
		if (Physics.Raycast(ray, out hitInfo, maxRange, (int)LayerIndex.CommonMasks.characterBodiesOrDefault | (int)LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			return hitInfo.collider.gameObject == target;
		}
		return true;
	}

	private bool WillHit(GameObject target)
	{
		Ray ray = new Ray(base.transform.position, base.transform.forward);
		RaycastHit hitInfo = default(RaycastHit);
		if (Physics.Raycast(ray, out hitInfo, maxRange, (int)LayerIndex.entityPrecise.mask | (int)LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			HurtBox component = hitInfo.collider.GetComponent<HurtBox>();
			if ((bool)component)
			{
				HealthComponent healthComponent = component.healthComponent;
				if ((bool)healthComponent)
				{
					return healthComponent.gameObject == target;
				}
			}
		}
		return false;
	}

	private GameObject FindTarget()
	{
		TeamIndex teamA = TeamIndex.Neutral;
		TeamComponent component = owner.GetComponent<TeamComponent>();
		if ((bool)component)
		{
			teamA = component.teamIndex;
		}
		Vector3 position = base.transform.position;
		float num = CalcDamage();
		float num2 = maxRange * maxRange;
		GameObject gameObject = null;
		GameObject result = null;
		float num3 = 0f;
		float num4 = float.PositiveInfinity;
		for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex++)
		{
			if (TeamManager.IsTeamEnemy(teamA, teamIndex))
			{
				ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
				for (int i = 0; i < teamMembers.Count; i++)
				{
					GameObject gameObject2 = teamMembers[i].gameObject;
					if (!((gameObject2.transform.position - position).sqrMagnitude <= num2))
					{
						continue;
					}
					HealthComponent component2 = teamMembers[i].GetComponent<HealthComponent>();
					if (!component2)
					{
						continue;
					}
					if (component2.health <= num)
					{
						if (component2.health > num3 && HasLoS(gameObject2))
						{
							gameObject = gameObject2;
							num3 = component2.health;
						}
					}
					else if (component2.health < num4 && HasLoS(gameObject2))
					{
						result = gameObject2;
						num4 = component2.health;
					}
				}
			}
		}
		if (!gameObject)
		{
			return result;
		}
		return gameObject;
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (!owner)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		InputBankTest component = owner.GetComponent<InputBankTest>();
		Vector3 vector = (component ? component.aimDirection : base.transform.forward);
		if ((bool)target)
		{
			vector = (target.transform.position - base.transform.position).normalized;
		}
		base.transform.forward = Vector3.RotateTowards(base.transform.forward, vector, MathF.PI / 180f * turnSpeed * Time.fixedDeltaTime, 0f);
		Vector3 vector2 = owner.transform.position + base.transform.rotation * localOffset;
		base.transform.position = Vector3.SmoothDamp(base.transform.position, vector2, ref velocity, positionSmoothTime, float.PositiveInfinity, Time.fixedDeltaTime);
		fireTimer -= Time.fixedDeltaTime;
		timeoutTimer -= Time.fixedDeltaTime;
		if (fireTimer <= 0f)
		{
			target = FindTarget();
			fireTimer = interval;
		}
		if ((bool)target && WillHit(target))
		{
			Vector3 normalized = (target.transform.position - base.transform.position).normalized;
			Fire(base.transform.position, normalized);
			ammo--;
			target = null;
			timeoutTimer = timeout;
		}
		if (ammo <= 0 || timeoutTimer <= 0f)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}
}
