using RoR2;
using UnityEngine;

namespace EntityStates.GolemMonster;

public class ChargeLaser : BaseState
{
	public static float baseDuration = 3f;

	public static float laserMaxWidth = 0.2f;

	public static GameObject effectPrefab;

	public static GameObject laserPrefab;

	public static string attackSoundString;

	private float duration;

	private uint chargePlayID;

	private GameObject chargeEffect;

	private GameObject laserEffect;

	private LineRenderer laserLineComponent;

	private Vector3 laserDirection;

	private Vector3 visualEndPosition;

	private float flashTimer;

	private bool laserOn;

	protected EffectManagerHelper _efh_Charge;

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargePlayID = 0u;
		chargeEffect = null;
		laserLineComponent = null;
		laserDirection = Vector3.zero;
		visualEndPosition = Vector3.zero;
		flashTimer = 0f;
		laserOn = false;
		_efh_Charge = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		SetAIUpdateFrequency(alwaysUpdate: true);
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		chargePlayID = Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleLaser");
				if ((bool)transform)
				{
					if ((bool)effectPrefab)
					{
						if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
						{
							chargeEffect = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
						}
						else
						{
							_efh_Charge = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform.position, transform.rotation);
							chargeEffect = _efh_Charge.gameObject;
						}
						chargeEffect.transform.parent = transform;
						ScaleParticleSystemDuration component2 = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
						if ((bool)component2)
						{
							component2.newDuration = duration;
						}
					}
					if ((bool)laserPrefab && laserEffect == null)
					{
						laserEffect = Object.Instantiate(laserPrefab, transform.position, transform.rotation);
					}
					if (laserEffect != null)
					{
						if (!laserEffect.activeInHierarchy)
						{
							laserEffect.SetActive(value: true);
						}
						if (laserEffect.transform.parent != transform)
						{
							laserEffect.transform.parent = transform;
						}
						laserLineComponent = laserEffect.GetComponent<LineRenderer>();
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
		flashTimer = 0f;
		laserOn = true;
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(chargePlayID);
		base.OnExit();
		SetAIUpdateFrequency(alwaysUpdate: false);
		if ((bool)chargeEffect)
		{
			if (!EffectManager.UsePools)
			{
				EntityState.Destroy(chargeEffect);
			}
			else if (_efh_Charge != null && _efh_Charge.OwningPool != null)
			{
				if (!_efh_Charge.OwningPool.IsObjectInPool(_efh_Charge))
				{
					_efh_Charge.OwningPool.ReturnObject(_efh_Charge);
				}
			}
			else
			{
				if (_efh_Charge != null)
				{
					Debug.LogFormat("ChargeLaser has no owning pool {0} {1}", base.gameObject.name, base.gameObject.GetInstanceID());
				}
				EntityState.Destroy(chargeEffect);
			}
		}
		if ((bool)laserEffect && laserEffect.activeInHierarchy)
		{
			laserEffect.SetActive(value: false);
		}
	}

	public override void Update()
	{
		base.Update();
		if (!laserEffect || !laserLineComponent)
		{
			return;
		}
		float num = 1000f;
		Ray aimRay = GetAimRay();
		Vector3 position = laserEffect.transform.parent.position;
		Vector3 point = aimRay.GetPoint(num);
		laserDirection = point - position;
		if (Physics.Raycast(aimRay, out var hitInfo, num, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask))
		{
			point = hitInfo.point;
		}
		laserLineComponent.SetPosition(0, position);
		laserLineComponent.SetPosition(1, point);
		float num2;
		if (duration - base.age > 0.5f)
		{
			num2 = base.age / duration;
		}
		else
		{
			flashTimer -= Time.deltaTime;
			if (flashTimer <= 0f)
			{
				laserOn = !laserOn;
				flashTimer = 1f / 30f;
			}
			num2 = (laserOn ? 1f : 0f);
		}
		num2 *= laserMaxWidth;
		laserLineComponent.startWidth = num2;
		laserLineComponent.endWidth = num2;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireLaser fireLaser = new FireLaser();
			fireLaser.laserDirection = laserDirection;
			outer.SetNextState(fireLaser);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
