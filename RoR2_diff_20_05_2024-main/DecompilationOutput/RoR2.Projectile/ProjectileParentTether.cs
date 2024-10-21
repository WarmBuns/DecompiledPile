using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
public class ProjectileParentTether : MonoBehaviour
{
	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private TeamIndex myTeamIndex;

	public float attackInterval = 1f;

	public float maxTetherRange = 20f;

	public float procCoefficient = 0.1f;

	public float damageCoefficient = 1f;

	public float raycastRadius;

	public float lifetime;

	public GameObject impactEffect;

	public GameObject tetherEffectPrefab;

	public ProjectileStickOnImpact stickOnImpact;

	private GameObject tetherEffectInstance;

	private GameObject tetherEffectInstanceEnd;

	private float attackTimer;

	private float lifetimeStopwatch;

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		attackTimer = 0f;
		UpdateTetherGraphic();
	}

	private void UpdateTetherGraphic()
	{
		if (ShouldIFire())
		{
			if ((bool)tetherEffectPrefab && !tetherEffectInstance)
			{
				tetherEffectInstance = Object.Instantiate(tetherEffectPrefab, base.transform.position, base.transform.rotation);
				tetherEffectInstance.transform.parent = base.transform;
				ChildLocator component = tetherEffectInstance.GetComponent<ChildLocator>();
				tetherEffectInstanceEnd = component.FindChild("LaserEnd").gameObject;
			}
			if ((bool)tetherEffectInstance)
			{
				Ray aimRay = GetAimRay();
				tetherEffectInstance.transform.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
				tetherEffectInstanceEnd.transform.position = aimRay.origin + aimRay.direction * GetRayDistance();
			}
		}
	}

	private float GetRayDistance()
	{
		if ((bool)projectileController.owner)
		{
			return (projectileController.owner.transform.position - base.transform.position).magnitude;
		}
		return 0f;
	}

	private Ray GetAimRay()
	{
		Ray result = default(Ray);
		result.origin = base.transform.position;
		result.direction = projectileController.owner.transform.position - result.origin;
		return result;
	}

	private bool ShouldIFire()
	{
		if ((bool)stickOnImpact)
		{
			return stickOnImpact.stuck;
		}
		return true;
	}

	private void Update()
	{
		UpdateTetherGraphic();
	}

	private void FixedUpdate()
	{
		if (ShouldIFire())
		{
			lifetimeStopwatch += Time.fixedDeltaTime;
		}
		if (lifetimeStopwatch > lifetime)
		{
			Object.Destroy(base.gameObject);
		}
		if (!projectileController.owner.transform || !ShouldIFire())
		{
			return;
		}
		myTeamIndex = (projectileController.teamFilter ? projectileController.teamFilter.teamIndex : TeamIndex.Neutral);
		attackTimer -= Time.fixedDeltaTime;
		if (attackTimer <= 0f)
		{
			Ray aimRay = GetAimRay();
			attackTimer = attackInterval;
			if (aimRay.direction.magnitude < maxTetherRange && NetworkServer.active)
			{
				BulletAttack bulletAttack = new BulletAttack();
				bulletAttack.owner = projectileController.owner;
				bulletAttack.origin = aimRay.origin;
				bulletAttack.aimVector = aimRay.direction;
				bulletAttack.minSpread = 0f;
				bulletAttack.damage = damageCoefficient * projectileDamage.damage;
				bulletAttack.force = 0f;
				bulletAttack.hitEffectPrefab = impactEffect;
				bulletAttack.isCrit = projectileDamage.crit;
				bulletAttack.radius = raycastRadius;
				bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
				bulletAttack.stopperMask = 0;
				bulletAttack.hitMask = LayerIndex.entityPrecise.mask;
				bulletAttack.procCoefficient = procCoefficient;
				bulletAttack.maxDistance = GetRayDistance();
				bulletAttack.Fire();
			}
		}
	}
}
