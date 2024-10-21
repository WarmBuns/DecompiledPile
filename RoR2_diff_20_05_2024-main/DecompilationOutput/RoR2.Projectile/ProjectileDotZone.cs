using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(HitBoxGroup))]
[RequireComponent(typeof(ProjectileController))]
public class ProjectileDotZone : MonoBehaviour, IProjectileImpactBehavior
{
	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	public float damageCoefficient;

	public AttackerFiltering attackerFiltering = AttackerFiltering.NeverHitSelf;

	public GameObject impactEffect;

	public Vector3 forceVector;

	public float overlapProcCoefficient = 1f;

	[Tooltip("The frequency (1/time) at which the overlap attack is tested. Higher values are more accurate but more expensive.")]
	public float fireFrequency = 1f;

	[Tooltip("The frequency  (1/time) at which the overlap attack is reset. Higher values means more frequent ticks of damage.")]
	public float resetFrequency = 20f;

	public float lifetime = 30f;

	[Tooltip("The event that runs at the start.")]
	public UnityEvent onBegin;

	[Tooltip("The event that runs at the start.")]
	public UnityEvent onEnd;

	private OverlapAttack attack;

	private float fireStopwatch;

	private float resetStopwatch;

	private float totalStopwatch;

	public string soundLoopString = "";

	public string soundLoopStopString = "";

	private void Start()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		ResetOverlap();
		onBegin.Invoke();
		if (!string.IsNullOrEmpty(soundLoopString))
		{
			Util.PlaySound(soundLoopString, base.gameObject);
		}
	}

	private void ResetOverlap()
	{
		attack = new OverlapAttack();
		attack.procChainMask = projectileController.procChainMask;
		attack.procCoefficient = projectileController.procCoefficient * overlapProcCoefficient;
		attack.attacker = projectileController.owner;
		attack.inflictor = base.gameObject;
		attack.teamIndex = projectileController.teamFilter.teamIndex;
		attack.attackerFiltering = attackerFiltering;
		attack.damage = damageCoefficient * projectileDamage.damage;
		attack.forceVector = forceVector + projectileDamage.force * base.transform.forward;
		attack.hitEffectPrefab = impactEffect;
		attack.isCrit = projectileDamage.crit;
		attack.damageColorIndex = projectileDamage.damageColorIndex;
		attack.damageType = projectileDamage.damageType;
		attack.hitBoxGroup = GetComponent<HitBoxGroup>();
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
	}

	public void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		totalStopwatch += Time.fixedDeltaTime;
		resetStopwatch += Time.fixedDeltaTime;
		fireStopwatch += Time.fixedDeltaTime;
		if (resetStopwatch >= 1f / resetFrequency)
		{
			ResetOverlap();
			resetStopwatch -= 1f / resetFrequency;
		}
		if (fireStopwatch >= 1f / fireFrequency)
		{
			attack.Fire();
			fireStopwatch -= 1f / fireFrequency;
		}
		if (lifetime > 0f && totalStopwatch >= lifetime)
		{
			onEnd.Invoke();
			if (!string.IsNullOrEmpty(soundLoopStopString))
			{
				Util.PlaySound(soundLoopStopString, base.gameObject);
			}
			Object.Destroy(base.gameObject);
		}
	}
}
