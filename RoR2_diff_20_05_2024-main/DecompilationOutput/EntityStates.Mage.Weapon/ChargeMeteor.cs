using RoR2;
using UnityEngine;

namespace EntityStates.Mage.Weapon;

public class ChargeMeteor : BaseState
{
	public static float baseChargeDuration;

	public static float baseDuration;

	public static GameObject areaIndicatorPrefab;

	public static float minMeteorRadius = 0f;

	public static float maxMeteorRadius = 10f;

	public static GameObject meteorEffect;

	public static float minDamageCoefficient;

	public static float maxDamageCoefficient;

	public static float procCoefficient;

	public static float force;

	public static GameObject muzzleflashEffect;

	private float stopwatch;

	private GameObject areaIndicatorInstance;

	private bool fireMeteor;

	private float radius;

	private float chargeDuration;

	private float duration;

	private BlastAttack blastAttack;

	private EffectData _effectData;

	private Vector3 _cachedScale = Vector3.one;

	private EffectManagerHelper _emh_areaIndicatorInstance;

	public override void Reset()
	{
		base.Reset();
		stopwatch = 0f;
		areaIndicatorInstance = null;
		fireMeteor = false;
		radius = 0f;
		chargeDuration = 0f;
		duration = 0f;
		if (blastAttack != null)
		{
			blastAttack.Reset();
		}
		if (_effectData != null)
		{
			_effectData.Reset();
		}
		_emh_areaIndicatorInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		chargeDuration = baseChargeDuration / attackSpeedStat;
		duration = baseDuration / attackSpeedStat;
		UpdateAreaIndicator();
	}

	private void UpdateAreaIndicator()
	{
		if ((bool)areaIndicatorInstance)
		{
			float maxDistance = 1000f;
			if (Physics.Raycast(GetAimRay(), out var hitInfo, maxDistance, LayerIndex.world.mask))
			{
				areaIndicatorInstance.transform.position = hitInfo.point;
				areaIndicatorInstance.transform.up = hitInfo.normal;
			}
		}
		else if (!EffectManager.ShouldUsePooledEffect(areaIndicatorPrefab))
		{
			areaIndicatorInstance = Object.Instantiate(areaIndicatorPrefab);
		}
		else
		{
			_emh_areaIndicatorInstance = EffectManager.GetAndActivatePooledEffect(areaIndicatorPrefab, Vector3.zero, Quaternion.identity);
			areaIndicatorInstance = _emh_areaIndicatorInstance.gameObject;
		}
		radius = Util.Remap(Mathf.Clamp01(stopwatch / chargeDuration), 0f, 1f, minMeteorRadius, maxMeteorRadius);
		_cachedScale.x = radius;
		_cachedScale.y = radius;
		_cachedScale.z = radius;
		areaIndicatorInstance.transform.localScale = _cachedScale;
	}

	public override void Update()
	{
		base.Update();
		UpdateAreaIndicator();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if ((stopwatch >= duration || base.inputBank.skill2.justReleased) && base.isAuthority)
		{
			fireMeteor = true;
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "Muzzle", transmit: false);
		if ((bool)areaIndicatorInstance)
		{
			if (fireMeteor)
			{
				float num = Util.Remap(Mathf.Clamp01(stopwatch / chargeDuration), 0f, 1f, minDamageCoefficient, maxDamageCoefficient);
				if (_effectData == null)
				{
					_effectData = new EffectData();
				}
				_effectData.origin = areaIndicatorInstance.transform.position;
				_effectData.scale = radius;
				EffectManager.SpawnEffect(meteorEffect, _effectData, transmit: true);
				if (blastAttack == null)
				{
					blastAttack = new BlastAttack();
				}
				blastAttack.radius = radius;
				blastAttack.procCoefficient = procCoefficient;
				blastAttack.position = areaIndicatorInstance.transform.position;
				blastAttack.attacker = base.gameObject;
				blastAttack.crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
				blastAttack.baseDamage = base.characterBody.damage * num;
				blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
				blastAttack.baseForce = force;
				blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
				blastAttack.Fire();
			}
			if (_emh_areaIndicatorInstance != null && _emh_areaIndicatorInstance.OwningPool != null)
			{
				_emh_areaIndicatorInstance.OwningPool.ReturnObject(_emh_areaIndicatorInstance);
			}
			else
			{
				EntityState.Destroy(areaIndicatorInstance.gameObject);
			}
			areaIndicatorInstance = null;
			_emh_areaIndicatorInstance = null;
		}
		base.OnExit();
	}
}
