using RoR2;
using UnityEngine;

namespace EntityStates.ClayBoss.ClayBossWeapon;

public class ChargeBombardment : BaseState
{
	public static float baseTotalDuration;

	public static float baseMaxChargeTime;

	public static int maxCharges;

	public static GameObject chargeEffectPrefab;

	public static string chargeLoopStartSoundString;

	public static string chargeLoopStopSoundString;

	public static int minGrenadeCount;

	public static int maxGrenadeCount;

	public static float minBonusBloom;

	public static float maxBonusBloom;

	private float stopwatch;

	private GameObject chargeInstance;

	private int charge;

	private float totalDuration;

	private float maxChargeTime;

	private EffectManagerHelper _emh_chargeInstance;

	private static int ChargeBombardmentStateHash = Animator.StringToHash("ChargeBombardment");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	public override void Reset()
	{
		base.Reset();
		stopwatch = 0f;
		chargeInstance = null;
		charge = 0;
		totalDuration = 0f;
		maxChargeTime = 0f;
		_emh_chargeInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		totalDuration = baseTotalDuration / attackSpeedStat;
		maxChargeTime = baseMaxChargeTime / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayAnimation("Gesture, Additive", ChargeBombardmentStateHash);
		Util.PlaySound(chargeLoopStartSoundString, base.gameObject);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("Muzzle");
				if ((bool)transform && (bool)chargeEffectPrefab)
				{
					if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
					{
						chargeInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
					}
					else
					{
						_emh_chargeInstance = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform.position, transform.rotation);
						chargeInstance = _emh_chargeInstance.gameObject;
					}
					chargeInstance.transform.parent = transform;
					ScaleParticleSystemDuration component2 = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component2)
					{
						component2.newDuration = totalDuration;
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(totalDuration);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		PlayAnimation("Gesture, Additive", EmptyStateHash);
		Util.PlaySound(chargeLoopStopSoundString, base.gameObject);
		if (_emh_chargeInstance != null && _emh_chargeInstance.OwningPool != null)
		{
			_emh_chargeInstance.OwningPool.ReturnObject(_emh_chargeInstance);
		}
		else
		{
			EntityState.Destroy(chargeInstance);
		}
		_emh_chargeInstance = null;
		chargeInstance = null;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		charge = Mathf.Min((int)(stopwatch / maxChargeTime * (float)maxCharges), maxCharges);
		float t = (float)charge / (float)maxCharges;
		float value = Mathf.Lerp(minBonusBloom, maxBonusBloom, t);
		base.characterBody.SetSpreadBloom(value);
		int grenadeCountMax = Mathf.FloorToInt(Mathf.Lerp(minGrenadeCount, maxGrenadeCount, t));
		if ((stopwatch >= totalDuration || !base.inputBank || !base.inputBank.skill1.down) && base.isAuthority)
		{
			FireBombardment fireBombardment = new FireBombardment();
			fireBombardment.grenadeCountMax = grenadeCountMax;
			outer.SetNextState(fireBombardment);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
