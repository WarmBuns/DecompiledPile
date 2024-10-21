using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2;

public class LightningStrikeInstance : MonoBehaviour
{
	public GameObject warningEffectPrefab;

	public GameObject impactEffectPrefab;

	public TeamComponent teamComponent;

	public float impactDelay;

	public float blastDamage;

	public float blastRadius;

	public float blastForce;

	public DamageTypeCombo blastDamageType;

	private Action ExecuteNextStep;

	private float stepTimer;

	private Vector3 impactPosition;

	private BlastAttack blastAttack;

	public bool Initialize(Vector3 _impactPosition, BlastAttack _blastInfo)
	{
		impactPosition = _impactPosition;
		if (!ConfirmGroundBelowImpactPoint())
		{
			return false;
		}
		base.transform.position = impactPosition;
		stepTimer = 0.0001f;
		if (impactDelay <= 0f)
		{
			ExecuteNextStep = HandleStrike;
		}
		else
		{
			ExecuteNextStep = HandleWarning;
		}
		if (_blastInfo != null)
		{
			blastAttack = _blastInfo;
		}
		else
		{
			blastAttack = new BlastAttack
			{
				attacker = null,
				baseDamage = blastDamage,
				baseForce = blastForce,
				radius = blastRadius,
				damageType = blastDamageType,
				teamIndex = teamComponent.teamIndex
			};
		}
		SetupDefaultBlastInfo(ref blastAttack);
		return true;
	}

	private bool ConfirmGroundBelowImpactPoint()
	{
		if (Physics.Raycast(new Ray(impactPosition + Vector3.up * 2f, Vector3.down), out var hitInfo, 200f, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault, QueryTriggerInteraction.Ignore))
		{
			impactPosition = hitInfo.point;
			return true;
		}
		return false;
	}

	private void SetupDefaultBlastInfo(ref BlastAttack _blastInfo)
	{
		_blastInfo.position = impactPosition;
		if (_blastInfo.attacker == null)
		{
			_blastInfo.attacker = base.gameObject;
		}
		_blastInfo.inflictor = base.gameObject;
		_blastInfo.attackerFiltering = AttackerFiltering.Default;
		_blastInfo.falloffModel = BlastAttack.FalloffModel.Linear;
		_blastInfo.bonusForce = Vector3.zero;
		_blastInfo.damageColorIndex = DamageColorIndex.Item;
		_blastInfo.procChainMask = default(ProcChainMask);
		_blastInfo.procCoefficient = 1f;
		if (_blastInfo.teamIndex == TeamIndex.Neutral)
		{
			_blastInfo.teamIndex = teamComponent.teamIndex;
		}
	}

	private void Update()
	{
		if (stepTimer > 0f)
		{
			stepTimer -= Time.deltaTime;
			if (stepTimer <= 0f)
			{
				ExecuteNextStep?.Invoke();
			}
		}
	}

	private void HandleWarning()
	{
		if (!LightningStormController.ShouldFireLighting())
		{
			ExecuteNextStep = HandleCleanup;
			stepTimer = 0.2f;
			return;
		}
		EffectManager.SpawnEffect(warningEffectPrefab, new EffectData
		{
			origin = impactPosition,
			scale = blastRadius
		}, transmit: true);
		stepTimer = impactDelay;
		ExecuteNextStep = HandleStrike;
	}

	private void HandleStrike()
	{
		if (!LightningStormController.ShouldFireLighting())
		{
			ExecuteNextStep = HandleCleanup;
			stepTimer = 0.2f;
			return;
		}
		if ((bool)impactEffectPrefab)
		{
			EffectData effectData = new EffectData
			{
				origin = impactPosition
			};
			EffectManager.SpawnEffect(impactEffectPrefab, effectData, transmit: true);
		}
		StrikeAboveImpactPosition();
		blastAttack.Fire();
		stepTimer = 1f;
		ExecuteNextStep = HandleCleanup;
	}

	private void StrikeAboveImpactPosition()
	{
		ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
		float num = 100f;
		float radius = this.blastAttack.radius;
		float num2 = this.blastAttack.radius * 1.3f;
		num2 *= num2;
		Vector3 vector = impactPosition;
		foreach (CharacterMaster item in readOnlyInstancesList)
		{
			if (item == null || item.GetBody() == null)
			{
				continue;
			}
			Vector3 corePosition = item.GetBody().corePosition;
			float num3 = Mathf.Abs(corePosition.y - impactPosition.y);
			if (num3 > num || num3 < radius)
			{
				continue;
			}
			vector.y = corePosition.y;
			if (!((corePosition - vector).sqrMagnitude > num2))
			{
				BlastAttack blastAttack = CopyBlastAttack(ref this.blastAttack);
				blastAttack.position = vector;
				if ((bool)impactEffectPrefab)
				{
					EffectData effectData = new EffectData
					{
						origin = blastAttack.position
					};
					EffectManager.SpawnEffect(impactEffectPrefab, effectData, transmit: true);
				}
				blastAttack.Fire();
			}
		}
	}

	private BlastAttack CopyBlastAttack(ref BlastAttack _source)
	{
		BlastAttack _blastInfo = new BlastAttack();
		_blastInfo.attacker = _source.attacker;
		_blastInfo.baseDamage = _source.baseDamage;
		_blastInfo.baseForce = _source.baseForce;
		_blastInfo.radius = _source.radius;
		_blastInfo.damageType = _source.damageType;
		SetupDefaultBlastInfo(ref _blastInfo);
		return _blastInfo;
	}

	private void HandleCleanup()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
