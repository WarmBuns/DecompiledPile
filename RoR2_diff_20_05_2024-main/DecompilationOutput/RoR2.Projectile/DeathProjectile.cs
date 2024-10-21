using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
[RequireComponent(typeof(ProjectileStickOnImpact))]
public class DeathProjectile : MonoBehaviour
{
	private ProjectileStickOnImpact projectileStickOnImpactController;

	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private HealthComponent healthComponent;

	public GameObject OnKillTickEffect;

	public TeamIndex teamIndex;

	public string activeSoundLoopString;

	public string exitSoundString;

	private float duration;

	private float fixedAge;

	public float baseDuration = 8f;

	public float radius = 500f;

	public GameObject rotateObject;

	private bool doneWithRemovalEvents;

	public float removalTime = 1f;

	private bool shouldStopSound;

	private void Awake()
	{
		projectileStickOnImpactController = GetComponent<ProjectileStickOnImpact>();
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		healthComponent = GetComponent<HealthComponent>();
		duration = baseDuration;
		fixedAge = 0f;
	}

	private void FixedUpdate()
	{
		fixedAge += Time.deltaTime;
		if (duration > 0f)
		{
			if (!(fixedAge >= 1f))
			{
				return;
			}
			if (projectileStickOnImpactController.stuck)
			{
				if ((bool)projectileController.owner)
				{
					RotateDoll(Random.Range(90f, 180f));
					SpawnTickEffect();
					if (NetworkServer.active)
					{
						DamageInfo damageInfo = new DamageInfo
						{
							attacker = projectileController.owner,
							crit = projectileDamage.crit,
							damage = projectileDamage.damage,
							position = base.transform.position,
							procCoefficient = projectileController.procCoefficient,
							damageType = projectileDamage.damageType,
							damageColorIndex = projectileDamage.damageColorIndex
						};
						HealthComponent victim = healthComponent;
						DamageReport damageReport = new DamageReport(damageInfo, victim, damageInfo.damage, healthComponent.combinedHealth);
						GlobalEventManager.instance.OnCharacterDeath(damageReport);
					}
				}
				duration -= 1f;
			}
			fixedAge = 0f;
		}
		else
		{
			if (!doneWithRemovalEvents)
			{
				doneWithRemovalEvents = true;
				rotateObject.GetComponent<ObjectScaleCurve>().enabled = true;
			}
			if (fixedAge >= removalTime)
			{
				Util.PlaySound(exitSoundString, base.gameObject);
				shouldStopSound = false;
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void OnDisable()
	{
		if (shouldStopSound)
		{
			Util.PlaySound(exitSoundString, base.gameObject);
			shouldStopSound = false;
		}
	}

	public void SpawnTickEffect()
	{
		EffectData effectData = new EffectData
		{
			origin = base.transform.position,
			rotation = Quaternion.identity
		};
		EffectManager.SpawnEffect(OnKillTickEffect, effectData, transmit: false);
	}

	public void PlayStickSoundLoop()
	{
		Util.PlaySound(activeSoundLoopString, base.gameObject);
		shouldStopSound = true;
	}

	public void RotateDoll(float rotationAmount)
	{
		rotateObject.transform.Rotate(new Vector3(0f, 0f, rotationAmount));
	}
}
