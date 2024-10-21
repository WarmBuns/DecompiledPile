using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(HitBoxGroup))]
[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
public class ProjectileOverlapAttack : MonoBehaviour, IProjectileImpactBehavior
{
	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	public float damageCoefficient;

	public GameObject impactEffect;

	public Vector3 forceVector;

	public float overlapProcCoefficient = 1f;

	public int maximumOverlapTargets = 100;

	public UnityEvent onServerHit;

	private OverlapAttack attack;

	public float fireFrequency = 60f;

	[Tooltip("If non-negative, the attack clears its hit memory at the specified interval.")]
	public float resetInterval = -1f;

	private float resetTimer;

	public bool requireDelayOnePhysicsFrame;

	[Header("Velocity Scaling")]
	public bool ScaleWithVelocity;

	public Rigidbody ScaleVelocitySource;

	private Vector3[] HitboxOriginalScales;

	private Transform[] HitboxTransforms;

	public Vector3 ScaleWithVelocityAxis = new Vector3(0f, 0f, 1f);

	private float fireTimer;

	private void Start()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		attack = new OverlapAttack();
		attack.procChainMask = projectileController.procChainMask;
		attack.procCoefficient = projectileController.procCoefficient * overlapProcCoefficient;
		attack.attacker = projectileController.owner;
		attack.inflictor = base.gameObject;
		attack.teamIndex = projectileController.teamFilter.teamIndex;
		attack.damage = damageCoefficient * projectileDamage.damage;
		attack.forceVector = forceVector + projectileDamage.force * base.transform.forward;
		attack.hitEffectPrefab = impactEffect;
		attack.isCrit = projectileDamage.crit;
		attack.damageColorIndex = projectileDamage.damageColorIndex;
		attack.damageType = projectileDamage.damageType;
		attack.procChainMask = projectileController.procChainMask;
		attack.maximumOverlapTargets = maximumOverlapTargets;
		attack.hitBoxGroup = GetComponent<HitBoxGroup>();
		int num = attack.hitBoxGroup.hitBoxes.Length;
		HitboxOriginalScales = new Vector3[num];
		HitboxTransforms = new Transform[num];
		for (int i = 0; i < num; i++)
		{
			HitboxTransforms[i] = attack.hitBoxGroup.hitBoxes[i].transform;
			HitboxOriginalScales[i] = HitboxTransforms[i].localScale;
		}
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
	}

	private void FixedUpdate()
	{
		MyFixedUpdate(Time.fixedDeltaTime);
	}

	public void MyFixedUpdate(float deltaTime)
	{
		if (!((!(projectileController != null)) ? NetworkServer.active : projectileController.CanProcessCollisionEvents()))
		{
			return;
		}
		if (resetInterval >= 0f)
		{
			resetTimer -= deltaTime;
			if (resetTimer <= 0f)
			{
				resetTimer = resetInterval;
				ResetOverlapAttack();
			}
		}
		fireTimer -= deltaTime;
		if (!(fireTimer <= 0f))
		{
			return;
		}
		if (ScaleWithVelocity)
		{
			float num = 1f / fireFrequency - fireTimer;
			if (num > 0f)
			{
				float num2 = num * ScaleVelocitySource.velocity.magnitude;
				Vector3 rhs = ScaleWithVelocityAxis * num2;
				int num3 = HitboxTransforms.Length;
				for (int i = 0; i < num3; i++)
				{
					HitboxTransforms[i].localScale = Vector3.Max(HitboxOriginalScales[i], rhs);
				}
			}
		}
		fireTimer = 1f / fireFrequency;
		attack.damage = damageCoefficient * projectileDamage.damage;
		if (attack.Fire())
		{
			onServerHit?.Invoke();
		}
	}

	public void ResetOverlapAttack()
	{
		attack.damageType = projectileDamage.damageType;
		attack.ResetIgnoredHealthComponents();
	}

	public void SetDamageCoefficient(float newDamageCoefficient)
	{
		damageCoefficient = newDamageCoefficient;
	}

	public void AddToDamageCoefficient(float bonusDamageCoefficient)
	{
		damageCoefficient += bonusDamageCoefficient;
	}
}
