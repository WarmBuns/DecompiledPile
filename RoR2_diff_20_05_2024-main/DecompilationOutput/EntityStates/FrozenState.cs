using RoR2;
using UnityEngine;

namespace EntityStates;

public class FrozenState : BaseState
{
	private float duration;

	private Animator modelAnimator;

	private TemporaryOverlayInstance temporaryOverlay;

	public float freezeDuration = 0.35f;

	public static GameObject frozenEffectPrefab;

	public static GameObject executeEffectPrefab;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.sfxLocator && base.sfxLocator.barkSound != "")
		{
			Util.PlaySound(base.sfxLocator.barkSound, base.gameObject);
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			CharacterModel component = modelTransform.GetComponent<CharacterModel>();
			if ((bool)component)
			{
				temporaryOverlay = TemporaryOverlayManager.AddOverlay(base.gameObject);
				temporaryOverlay.duration = freezeDuration;
				temporaryOverlay.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matIsFrozen");
				temporaryOverlay.AddToCharacterModel(component);
			}
		}
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.enabled = false;
			duration = freezeDuration;
			EffectManager.SpawnEffect(frozenEffectPrefab, new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = (base.characterBody ? base.characterBody.radius : 1f)
			}, transmit: false);
		}
		if ((bool)base.rigidbody && !base.rigidbody.isKinematic)
		{
			base.rigidbody.velocity = Vector3.zero;
			if ((bool)base.rigidbodyMotor)
			{
				base.rigidbodyMotor.moveVector = Vector3.zero;
			}
		}
		base.healthComponent.isInFrozenState = true;
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterDirection.forward;
		}
	}

	public override void OnExit()
	{
		if ((bool)modelAnimator)
		{
			modelAnimator.enabled = true;
		}
		if (temporaryOverlay != null)
		{
			temporaryOverlay.Destroy();
			temporaryOverlay = null;
		}
		EffectManager.SpawnEffect(frozenEffectPrefab, new EffectData
		{
			origin = base.characterBody.corePosition,
			scale = (base.characterBody ? base.characterBody.radius : 1f)
		}, transmit: false);
		base.healthComponent.isInFrozenState = false;
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
