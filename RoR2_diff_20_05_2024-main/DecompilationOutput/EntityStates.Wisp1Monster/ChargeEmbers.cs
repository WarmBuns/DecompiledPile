using RoR2;
using UnityEngine;

namespace EntityStates.Wisp1Monster;

public class ChargeEmbers : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargeEffectPrefab;

	public static GameObject laserEffectPrefab;

	public static string attackString;

	private float duration;

	private float stopwatch;

	private uint soundID;

	private GameObject chargeEffectInstance;

	private GameObject laserEffectInstance;

	private LineRenderer laserEffectInstanceLineRenderer;

	private bool lineRendererNull;

	protected EffectManagerHelper _emh_chargeEffect;

	protected EffectManagerHelper _emh_laserEffect;

	private Color startColor = Color.white;

	private static int ChargeAttack1StateHash = Animator.StringToHash("ChargeAttack1");

	private static int ChargeAttack1ParamHash = Animator.StringToHash("ChargeAttack1.playbackRate");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeEffectInstance = null;
		laserEffectInstance = null;
		laserEffectInstanceLineRenderer = null;
		lineRendererNull = true;
		_emh_chargeEffect = null;
		_emh_laserEffect = null;
		startColor = Color.white;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		SetAIUpdateFrequency(alwaysUpdate: true);
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		soundID = Util.PlayAttackSpeedSound(attackString, base.gameObject, attackSpeedStat);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("Muzzle");
				if ((bool)transform)
				{
					if ((bool)chargeEffectPrefab)
					{
						if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
						{
							chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
						}
						else
						{
							_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform.position, transform.rotation);
							chargeEffectInstance = _emh_chargeEffect.gameObject;
						}
						chargeEffectInstance.transform.parent = transform;
						ScaleParticleSystemDuration component2 = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
						if ((bool)component2)
						{
							component2.newDuration = duration;
						}
					}
					if ((bool)laserEffectPrefab)
					{
						if (!EffectManager.ShouldUsePooledEffect(laserEffectPrefab))
						{
							laserEffectInstance = Object.Instantiate(laserEffectPrefab, transform.position, transform.rotation);
						}
						else
						{
							_emh_laserEffect = EffectManager.GetAndActivatePooledEffect(laserEffectPrefab, transform.position, transform.rotation);
							laserEffectInstance = _emh_laserEffect.gameObject;
						}
						laserEffectInstance.transform.parent = transform;
						laserEffectInstanceLineRenderer = laserEffectInstance.GetComponent<LineRenderer>();
						lineRendererNull = false;
					}
				}
			}
		}
		PlayAnimation("Body", ChargeAttack1StateHash, ChargeAttack1ParamHash, duration);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		SetAIUpdateFrequency(alwaysUpdate: false);
		AkSoundEngine.StopPlayingID(soundID);
		if ((bool)chargeEffectInstance)
		{
			if (!EffectManager.UsePools)
			{
				EntityState.Destroy(chargeEffectInstance);
			}
			else if (_emh_chargeEffect != null && _emh_chargeEffect.OwningPool != null)
			{
				_emh_chargeEffect.OwningPool.ReturnObject(_emh_chargeEffect);
			}
			else
			{
				EntityState.Destroy(chargeEffectInstance);
			}
			chargeEffectInstance = null;
			_emh_chargeEffect = null;
		}
		if ((bool)laserEffectInstance)
		{
			if (!EffectManager.UsePools)
			{
				EntityState.Destroy(laserEffectInstance);
			}
			else if (_emh_laserEffect != null && _emh_laserEffect.OwningPool != null)
			{
				_emh_laserEffect.OwningPool.ReturnObject(_emh_laserEffect);
			}
			else
			{
				EntityState.Destroy(laserEffectInstance);
			}
			laserEffectInstance = null;
			_emh_laserEffect = null;
			lineRendererNull = true;
		}
	}

	public override void Update()
	{
		base.Update();
		if (!lineRendererNull)
		{
			if (!laserEffectInstanceLineRenderer)
			{
				lineRendererNull = true;
				return;
			}
			Ray aimRay = GetAimRay();
			float distance = 50f;
			Vector3 origin = aimRay.origin;
			Vector3 point = aimRay.GetPoint(distance);
			laserEffectInstanceLineRenderer.SetPosition(0, origin);
			laserEffectInstanceLineRenderer.SetPosition(1, point);
			startColor.a = stopwatch / duration;
			laserEffectInstanceLineRenderer.startColor = startColor;
			laserEffectInstanceLineRenderer.endColor = Color.clear;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextState(new FireEmbers());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
