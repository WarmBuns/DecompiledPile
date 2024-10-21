using RoR2;
using UnityEngine;

namespace EntityStates.JellyfishMonster;

public class JellyNova : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargingEffectPrefab;

	public static GameObject novaEffectPrefab;

	public static string chargingSoundString;

	public static string novaSoundString;

	public static float novaDamageCoefficient;

	public static float novaRadius;

	public static float novaForce;

	private bool hasExploded;

	private float duration;

	private float stopwatch;

	private GameObject chargeEffect;

	private PrintController printController;

	private uint soundID;

	private Vector3 _cachedScale = Vector3.one;

	private BlastAttack attack;

	private EffectData _effectData;

	private EffectManagerHelper _emh_chargeEffect;

	public override void Reset()
	{
		base.Reset();
		hasExploded = false;
		duration = 0f;
		chargeEffect = null;
		printController = null;
		soundID = 0u;
		if (attack != null)
		{
			attack.Reset();
		}
		if (_effectData != null)
		{
			_effectData.Reset();
		}
		_emh_chargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayCrossfade("Body", "Nova", "Nova.playbackRate", duration, 0.1f);
		soundID = Util.PlaySound(chargingSoundString, base.gameObject);
		if ((bool)chargingEffectPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargingEffectPrefab))
			{
				chargeEffect = Object.Instantiate(chargingEffectPrefab, base.transform.position, base.transform.rotation);
			}
			else
			{
				_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(chargingEffectPrefab, base.transform.position, base.transform.rotation);
				chargeEffect = _emh_chargeEffect.gameObject;
			}
			chargeEffect.transform.parent = base.transform;
			_cachedScale.x = novaRadius;
			_cachedScale.y = novaRadius;
			_cachedScale.z = novaRadius;
			chargeEffect.transform.localScale = _cachedScale;
			chargeEffect.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
		}
		if ((bool)modelTransform)
		{
			printController = modelTransform.GetComponent<PrintController>();
			if ((bool)printController)
			{
				printController.enabled = true;
				printController.printTime = duration;
			}
		}
	}

	protected void DestroyChargeEffect()
	{
		if ((bool)chargeEffect)
		{
			if (_emh_chargeEffect != null && _emh_chargeEffect.OwningPool != null)
			{
				_emh_chargeEffect.OwningPool.ReturnObject(_emh_chargeEffect);
			}
			else
			{
				EntityState.Destroy(chargeEffect);
			}
			_emh_chargeEffect = null;
			chargeEffect = null;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		AkSoundEngine.StopPlayingID(soundID);
		DestroyChargeEffect();
		if ((bool)printController)
		{
			printController.enabled = false;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority && !hasExploded)
		{
			Detonate();
		}
	}

	private void Detonate()
	{
		hasExploded = true;
		Util.PlaySound(novaSoundString, base.gameObject);
		if ((bool)base.modelLocator)
		{
			if ((bool)base.modelLocator.modelBaseTransform)
			{
				EntityState.Destroy(base.modelLocator.modelBaseTransform.gameObject);
			}
			if ((bool)base.modelLocator.modelTransform)
			{
				EntityState.Destroy(base.modelLocator.modelTransform.gameObject);
			}
		}
		DestroyChargeEffect();
		if ((bool)novaEffectPrefab)
		{
			if (_effectData == null)
			{
				_effectData = new EffectData();
			}
			_effectData.origin = base.transform.position;
			_effectData.scale = novaRadius;
			EffectManager.SpawnEffect(novaEffectPrefab, _effectData, transmit: true);
		}
		if (attack == null)
		{
			attack = new BlastAttack();
		}
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
		attack.baseDamage = damageStat * novaDamageCoefficient;
		attack.baseForce = novaForce;
		attack.position = base.transform.position;
		attack.radius = novaRadius;
		attack.procCoefficient = 2f;
		attack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		attack.Fire();
		if ((bool)base.healthComponent)
		{
			base.healthComponent.Suicide();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
