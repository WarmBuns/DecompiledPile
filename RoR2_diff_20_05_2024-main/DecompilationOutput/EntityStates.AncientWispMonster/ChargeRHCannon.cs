using RoR2;
using UnityEngine;

namespace EntityStates.AncientWispMonster;

public class ChargeRHCannon : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject effectPrefab;

	private float duration;

	private GameObject chargeEffectLeft;

	private GameObject chargeEffectRight;

	private EffectManagerHelper _emh_chargeEffectRight;

	private static int chargeRHCannonHash = Animator.StringToHash("ChargeRHCannon");

	private static int chargeRHCannonPlaybackHash = Animator.StringToHash("ChargeRHCannon.playbackRate");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeEffectLeft = null;
		chargeEffectRight = null;
		_emh_chargeEffectRight = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayAnimation("Gesture", chargeRHCannonHash, chargeRHCannonPlaybackHash, duration);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component && (bool)effectPrefab)
			{
				Transform transform = component.FindChild("MuzzleRight");
				if ((bool)transform)
				{
					if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
					{
						chargeEffectRight = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
					}
					else
					{
						_emh_chargeEffectRight = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform.position, transform.rotation);
						chargeEffectRight = _emh_chargeEffectRight.gameObject;
					}
					chargeEffectRight.transform.parent = transform;
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
		base.OnExit();
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
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireRHCannon nextState = new FireRHCannon();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
