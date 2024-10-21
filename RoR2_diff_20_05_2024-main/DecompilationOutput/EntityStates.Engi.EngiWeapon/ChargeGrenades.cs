using RoR2;
using UnityEngine;

namespace EntityStates.Engi.EngiWeapon;

public class ChargeGrenades : BaseState
{
	public static float baseTotalDuration;

	public static float baseMaxChargeTime;

	public static int maxCharges;

	public static GameObject chargeEffectPrefab;

	public static string chargeStockSoundString;

	public static string chargeLoopStartSoundString;

	public static string chargeLoopStopSoundString;

	public static int minGrenadeCount;

	public static int maxGrenadeCount;

	public static float minBonusBloom;

	public static float maxBonusBloom;

	private GameObject chargeLeftInstance;

	private GameObject chargeRightInstance;

	private int charge;

	private int lastCharge;

	private float totalDuration;

	private float maxChargeTime;

	private EffectManagerHelper _emh_chargeLeftInstance;

	private EffectManagerHelper _emh_chargeRightInstance;

	private static int ChargeGrenadesStateHash = Animator.StringToHash("ChargeGrenades");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	public override void Reset()
	{
		base.Reset();
		chargeLeftInstance = null;
		chargeRightInstance = null;
		charge = 0;
		lastCharge = 0;
		totalDuration = 0f;
		maxChargeTime = 0f;
		_emh_chargeLeftInstance = null;
		_emh_chargeRightInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		totalDuration = baseTotalDuration / attackSpeedStat;
		maxChargeTime = baseMaxChargeTime / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayAnimation("Gesture, Additive", ChargeGrenadesStateHash);
		Util.PlaySound(chargeLoopStartSoundString, base.gameObject);
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("MuzzleLeft");
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
			{
				chargeLeftInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			}
			else
			{
				_emh_chargeLeftInstance = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform.position, transform.rotation);
				chargeLeftInstance = _emh_chargeLeftInstance.gameObject;
			}
			chargeLeftInstance.transform.parent = transform;
			ScaleParticleSystemDuration component2 = chargeLeftInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component2)
			{
				component2.newDuration = totalDuration;
			}
		}
		Transform transform2 = component.FindChild("MuzzleRight");
		if ((bool)transform2 && (bool)chargeEffectPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
			{
				chargeRightInstance = Object.Instantiate(chargeEffectPrefab, transform2.position, transform2.rotation);
			}
			else
			{
				_emh_chargeRightInstance = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform2.position, transform2.rotation);
				chargeRightInstance = _emh_chargeRightInstance.gameObject;
			}
			chargeRightInstance.transform.parent = transform2;
			ScaleParticleSystemDuration component3 = chargeRightInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component3)
			{
				component3.newDuration = totalDuration;
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		PlayAnimation("Gesture, Additive", EmptyStateHash);
		Util.PlaySound(chargeLoopStopSoundString, base.gameObject);
		if (chargeLeftInstance != null)
		{
			if (_emh_chargeLeftInstance != null && _emh_chargeLeftInstance.OwningPool != null)
			{
				_emh_chargeLeftInstance.OwningPool.ReturnObject(_emh_chargeLeftInstance);
			}
			else
			{
				EntityState.Destroy(chargeLeftInstance);
			}
			chargeLeftInstance = null;
			_emh_chargeLeftInstance = null;
		}
		if (chargeRightInstance != null)
		{
			if (_emh_chargeRightInstance != null && _emh_chargeRightInstance.OwningPool != null)
			{
				_emh_chargeRightInstance.OwningPool.ReturnObject(_emh_chargeRightInstance);
			}
			else
			{
				EntityState.Destroy(chargeRightInstance);
			}
			chargeRightInstance = null;
			_emh_chargeRightInstance = null;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		lastCharge = charge;
		charge = Mathf.Min((int)(base.fixedAge / maxChargeTime * (float)maxCharges), maxCharges);
		float t = (float)charge / (float)maxCharges;
		float value = Mathf.Lerp(minBonusBloom, maxBonusBloom, t);
		base.characterBody.SetSpreadBloom(value);
		int num = Mathf.FloorToInt(Mathf.Lerp(minGrenadeCount, maxGrenadeCount, t));
		if (lastCharge < charge)
		{
			Util.PlaySound(chargeStockSoundString, base.gameObject, "engiM1_chargePercent", 100f * ((float)(num - 1) / (float)maxGrenadeCount));
		}
		if ((base.fixedAge >= totalDuration || !base.inputBank || !base.inputBank.skill1.down) && base.isAuthority)
		{
			FireGrenades fireGrenades = new FireGrenades();
			fireGrenades.grenadeCountMax = num;
			outer.SetNextState(fireGrenades);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
