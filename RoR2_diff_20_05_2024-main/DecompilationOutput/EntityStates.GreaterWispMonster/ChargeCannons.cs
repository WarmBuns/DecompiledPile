using RoR2;
using UnityEngine;

namespace EntityStates.GreaterWispMonster;

public class ChargeCannons : BaseState
{
	[SerializeField]
	public float baseDuration = 3f;

	[SerializeField]
	public GameObject effectPrefab;

	[SerializeField]
	public string attackString;

	protected float duration;

	private GameObject chargeEffectLeft;

	private GameObject chargeEffectRight;

	private const float soundDuration = 2f;

	private uint soundID;

	private EffectManagerHelper _emh_chargeEffectLeft;

	private EffectManagerHelper _emh_chargeEffectRight;

	private static int ChargeCannonsStateHash = Animator.StringToHash("ChargeCannons");

	private static int ChargeCannonsParamHash = Animator.StringToHash("ChargeCannons.playbackRate");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeEffectLeft = null;
		chargeEffectRight = null;
		_emh_chargeEffectLeft = null;
		_emh_chargeEffectRight = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		soundID = Util.PlayAttackSpeedSound(attackString, base.gameObject, attackSpeedStat * (2f / baseDuration));
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		GetModelAnimator();
		PlayAnimation("Gesture", ChargeCannonsStateHash, ChargeCannonsParamHash, duration);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component && (bool)effectPrefab)
			{
				Transform transform = component.FindChild("MuzzleLeft");
				Transform transform2 = component.FindChild("MuzzleRight");
				if ((bool)transform)
				{
					if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
					{
						chargeEffectLeft = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
					}
					else
					{
						_emh_chargeEffectLeft = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform.position, transform.rotation);
						chargeEffectLeft = _emh_chargeEffectLeft.gameObject;
					}
					chargeEffectLeft.transform.parent = transform;
					ScaleParticleSystemDuration component2 = chargeEffectLeft.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component2)
					{
						component2.newDuration = duration;
					}
				}
				if ((bool)transform2)
				{
					if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
					{
						chargeEffectRight = Object.Instantiate(effectPrefab, transform2.position, transform2.rotation);
					}
					else
					{
						_emh_chargeEffectRight = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform2.position, transform2.rotation);
						chargeEffectRight = _emh_chargeEffectRight.gameObject;
					}
					chargeEffectRight.transform.parent = transform2;
					ScaleParticleSystemDuration component3 = chargeEffectRight.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component3)
					{
						component3.newDuration = duration;
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void OnExit()
	{
		if (base.fixedAge < duration - 0.1f)
		{
			AkSoundEngine.StopPlayingID(soundID);
		}
		PlayAnimation("Gesture", EmptyStateHash);
		if (_emh_chargeEffectLeft != null && _emh_chargeEffectLeft.OwningPool != null)
		{
			_emh_chargeEffectLeft.OwningPool.ReturnObject(_emh_chargeEffectLeft);
		}
		else
		{
			EntityState.Destroy(chargeEffectLeft);
		}
		chargeEffectLeft = null;
		_emh_chargeEffectLeft = null;
		if (_emh_chargeEffectRight != null && _emh_chargeEffectRight.OwningPool != null)
		{
			_emh_chargeEffectRight.OwningPool.ReturnObject(_emh_chargeEffectRight);
		}
		else
		{
			EntityState.Destroy(chargeEffectRight);
		}
		chargeEffectRight = null;
		_emh_chargeEffectRight = null;
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireCannons nextState = new FireCannons();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
