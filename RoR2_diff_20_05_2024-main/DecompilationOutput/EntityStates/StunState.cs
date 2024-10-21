using System;
using RoR2;
using UnityEngine;

namespace EntityStates;

public class StunState : BaseState
{
	private float duration;

	private GameObject stunVfxInstance;

	public float stunDuration = 0.35f;

	public static GameObject stunVfxPrefab;

	protected EffectManagerHelper _efhStunEffect;

	public float timeRemaining => Math.Max(duration - base.fixedAge, 0f);

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		stunVfxInstance = null;
		stunDuration = 0.35f;
		_efhStunEffect = null;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Stun;
	}

	public void ExtendStun(float durationDelta)
	{
		duration += durationDelta;
		PlayStunAnimation();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.sfxLocator && base.sfxLocator.barkSound != "")
		{
			Util.PlaySound(base.sfxLocator.barkSound, base.gameObject);
		}
		PlayStunAnimation();
		if ((bool)base.characterBody)
		{
			base.characterBody.isSprinting = false;
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterDirection.forward;
		}
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = Vector3.zero;
		}
	}

	private void PlayStunAnimation()
	{
		Animator modelAnimator = GetModelAnimator();
		if (!modelAnimator)
		{
			return;
		}
		int layerIndex = modelAnimator.GetLayerIndex("Body");
		modelAnimator.CrossFadeInFixedTime((UnityEngine.Random.Range(0, 2) == 0) ? "Hurt1" : "Hurt2", 0.1f);
		modelAnimator.Update(0f);
		AnimatorStateInfo nextAnimatorStateInfo = modelAnimator.GetNextAnimatorStateInfo(layerIndex);
		duration = Mathf.Max(duration, Mathf.Max(stunDuration, nextAnimatorStateInfo.length));
		if (stunDuration >= 0f)
		{
			if ((bool)stunVfxInstance)
			{
				ReleaseStunVFX();
			}
			if (!EffectManager.ShouldUsePooledEffect(stunVfxPrefab))
			{
				stunVfxInstance = UnityEngine.Object.Instantiate(stunVfxPrefab, base.transform);
			}
			else
			{
				_efhStunEffect = EffectManager.GetAndActivatePooledEffect(stunVfxPrefab, base.transform);
				stunVfxInstance = _efhStunEffect.gameObject;
			}
			ScaleParticleSystemDuration component = stunVfxInstance.GetComponent<ScaleParticleSystemDuration>();
			component.newDuration = duration;
			component.UpdateDuration();
		}
	}

	public override void OnExit()
	{
		ReleaseStunVFX();
		base.OnExit();
	}

	private void ReleaseStunVFX()
	{
		if (!stunVfxInstance)
		{
			return;
		}
		if (!EffectManager.UsePools)
		{
			EntityState.Destroy(stunVfxInstance);
		}
		else if (_efhStunEffect != null && _efhStunEffect.OwningPool != null)
		{
			if (!_efhStunEffect.OwningPool.IsObjectInPool(_efhStunEffect))
			{
				_efhStunEffect.OwningPool.ReturnObject(_efhStunEffect);
			}
		}
		else
		{
			if (_efhStunEffect != null)
			{
				Debug.LogFormat("StunEffect has no owning pool {0} {1}", base.gameObject.name, base.gameObject.GetInstanceID());
			}
			EntityState.Destroy(stunVfxInstance);
		}
		_efhStunEffect = null;
		stunVfxInstance = null;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
