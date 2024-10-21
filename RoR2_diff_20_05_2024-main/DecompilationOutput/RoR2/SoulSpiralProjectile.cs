using System;
using System.Collections.Generic;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class SoulSpiralProjectile : MonoBehaviour
{
	public static List<SoulSpiralProjectile> unassignedSoulSpirals = new List<SoulSpiralProjectile>();

	[Header("Basic setup")]
	[SerializeField]
	public Vector3 offset;

	[SerializeField]
	public float initialDegreesFromOwnerForward;

	[SerializeField]
	public float degreesPerSecond;

	[SerializeField]
	public float radius;

	[SerializeField]
	public Vector3 planeNormal = Vector3.up;

	[SerializeField]
	public int hitStackPerBuff = 1;

	[SerializeField]
	[Tooltip("How much from 'baseline' can we move up or down during our sine wave")]
	public float verticalSineWaveOffset = 0.25f;

	[SerializeField]
	public float sineWavesPerSecond = 1.5f;

	[Header("Easing speeds")]
	[SerializeField]
	[Space(20f)]
	[Tooltip("How long it takes the projectile to ease into its orbit from starting position.")]
	public float easeInDuration;

	[SerializeField]
	[Tooltip("How long it takes the projectile to transition from base speed to boosted speed")]
	public float boostEaseDuration;

	[Header("Boost variables")]
	[SerializeField]
	[Space(20f)]
	[Tooltip("On the last hit, the projectile does more damage.")]
	public float lastHitDamageMultiplier;

	[SerializeField]
	[Tooltip("How long will the spiral spin after being boosted")]
	public float boostDuration;

	[SerializeField]
	[Tooltip("Coefficient for calculating Speed boost - multiplied against the # of boosts we have.")]
	public float speedBoostCoefficient;

	[SerializeField]
	[Tooltip("How much should baseDamage be multipled by, if the speed is 100% faster than base.")]
	public float damageMultiplier;

	[SerializeField]
	[Tooltip("Triggered when the orbs are 'boosted'.")]
	private UnityEvent OnOrbBoost;

	private SeekerSoulSpiralManager spiralManager;

	[HideInInspector]
	public ProjectileController projectileController;

	[HideInInspector]
	public ProjectileDamage projectileDamage;

	[HideInInspector]
	public ProjectileOverlapLimitHits projectileOverlapLimitHits;

	private SoulSpiralGhost _soulSpiralGhost;

	private Rigidbody rigidBody;

	private Transform ownerTransform;

	private float initialStartTimeStamp;

	private Vector3 initialRadialDirection;

	private float prevDamage;

	private float baseDamage;

	private int baseHitLimit;

	private const float twoPi = MathF.PI * 2f;

	private SoulSpiralGhost soulSpiralGhost
	{
		get
		{
			if (_soulSpiralGhost == null)
			{
				if (projectileController == null || projectileController.ghost == null)
				{
					return null;
				}
				_soulSpiralGhost = projectileController.ghost.GetComponent<SoulSpiralGhost>();
			}
			return _soulSpiralGhost;
		}
		set
		{
			_soulSpiralGhost = value;
		}
	}

	private void OnEnable()
	{
		unassignedSoulSpirals.Add(this);
		spiralManager = null;
		projectileController = projectileController ?? GetComponent<ProjectileController>();
		rigidBody = rigidBody ?? GetComponent<Rigidbody>();
		projectileDamage = projectileDamage ?? GetComponent<ProjectileDamage>();
		projectileOverlapLimitHits = projectileOverlapLimitHits ?? GetComponent<ProjectileOverlapLimitHits>();
		baseHitLimit = (projectileOverlapLimitHits ? projectileOverlapLimitHits.hitLimit : 3);
		if (sineWavesPerSecond < 0f)
		{
			sineWavesPerSecond = Mathf.Abs(sineWavesPerSecond);
		}
		if (sineWavesPerSecond == 0f)
		{
			sineWavesPerSecond = 1f;
		}
	}

	private void OnDisable()
	{
		if (unassignedSoulSpirals.Contains(this))
		{
			unassignedSoulSpirals.Remove(this);
		}
	}

	public void AssignSpiralOwner(SeekerSoulSpiralManager _spiralManager)
	{
		spiralManager = _spiralManager;
		if (unassignedSoulSpirals.Contains(this))
		{
			unassignedSoulSpirals.Remove(this);
		}
		ownerTransform = _spiralManager.seekerBody.transform;
		SetDefaultVariables();
	}

	private void SetDefaultVariables()
	{
		if (hitStackPerBuff < 1)
		{
			hitStackPerBuff = 1;
		}
		projectileOverlapLimitHits.hitLimit = baseHitLimit + spiralManager.seekerBody.GetBuffCount(DLC2Content.Buffs.ChakraBuff.buffIndex) * hitStackPerBuff;
		initialStartTimeStamp = Time.fixedTime;
		initialRadialDirection = Quaternion.AngleAxis(initialDegreesFromOwnerForward, planeNormal) * ownerTransform.forward;
		baseDamage = projectileDamage.damage;
		soulSpiralGhost = projectileController.ghost?.GetComponent<SoulSpiralGhost>();
	}

	public void OnLastHit()
	{
		soulSpiralGhost?.OnLastHit();
	}

	public void Boost()
	{
		soulSpiralGhost?.Boost();
	}

	public void EndBoost()
	{
		soulSpiralGhost?.EndBoost();
	}

	public void UpdateProjectile(float currentTime, float degreesRotated, float damageMultiplier, bool doBoost = false, bool doEndBoost = false)
	{
		if (!(ownerTransform == null))
		{
			if (doBoost)
			{
				OnOrbBoost?.Invoke();
				soulSpiralGhost?.Boost();
			}
			if (doEndBoost)
			{
				soulSpiralGhost?.EndBoost();
			}
			Vector3 vector = ownerTransform.position + offset + Quaternion.AngleAxis(degreesRotated, planeNormal) * initialRadialDirection * radius;
			float num = verticalSineWaveOffset;
			float num2 = (degreesRotated + initialDegreesFromOwnerForward) / degreesPerSecond;
			float num3 = Mathf.Cos(MathF.PI * 2f * num2 * sineWavesPerSecond);
			vector.y += num3 * num;
			if (currentTime - initialStartTimeStamp <= easeInDuration)
			{
				float num4 = currentTime - initialStartTimeStamp;
				vector = Vector3.Lerp(base.transform.position, vector, num4 / easeInDuration);
			}
			if ((bool)rigidBody)
			{
				rigidBody.MovePosition(vector);
			}
			else
			{
				base.transform.position = vector;
			}
			float num5 = baseDamage * damageMultiplier;
			if (projectileOverlapLimitHits.IsLastHit())
			{
				num5 *= lastHitDamageMultiplier;
			}
			if (num5 != prevDamage)
			{
				projectileDamage.damage = num5;
				prevDamage = num5;
			}
		}
	}

	public void SetInitialDegreesFromOwnerForward(float degrees, int positionIndex)
	{
		initialDegreesFromOwnerForward = degrees;
		if ((bool)ownerTransform)
		{
			initialRadialDirection = Quaternion.AngleAxis(initialDegreesFromOwnerForward, planeNormal) * ownerTransform.forward;
		}
	}

	public void DestroySpiral()
	{
		EffectManagerHelper component = GetComponent<EffectManagerHelper>();
		if ((bool)component && component.OwningPool != null)
		{
			component.OwningPool.ReturnObject(component);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}
}
