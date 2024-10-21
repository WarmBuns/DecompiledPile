using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates;

public class GenericCharacterDeath : BaseState
{
	private static readonly float bodyPreservationDuration = 1f;

	private static readonly float hardCutoffDuration = 10f;

	private static readonly float maxFallDuration = 4f;

	private static readonly float minTimeToKeepBodyForNetworkMessages = 0.5f;

	public static GameObject voidDeathEffect;

	private float restStopwatch;

	private float fallingStopwatch;

	private bool bodyMarkedForDestructionServer;

	private CameraTargetParams.AimRequest aimRequest;

	protected Transform cachedModelTransform { get; private set; }

	protected bool isBrittle { get; private set; }

	protected bool isVoidDeath { get; private set; }

	protected bool isPlayerDeath { get; private set; }

	protected virtual bool shouldAutoDestroy => true;

	protected virtual float GetDeathAnimationCrossFadeDuration()
	{
		return 0.1f;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		bodyMarkedForDestructionServer = false;
		cachedModelTransform = (base.modelLocator ? base.modelLocator.modelTransform : null);
		isBrittle = (bool)base.characterBody && base.characterBody.isGlass;
		isVoidDeath = (bool)base.healthComponent && (ulong)(base.healthComponent.killingDamageType & DamageType.VoidDeath) != 0;
		isPlayerDeath = (bool)base.characterBody.master && base.characterBody.master.GetComponent<PlayerCharacterMasterController>() != null;
		if (isVoidDeath)
		{
			if ((bool)base.characterBody && base.isAuthority)
			{
				EffectManager.SpawnEffect(voidDeathEffect, new EffectData
				{
					origin = base.characterBody.corePosition,
					scale = base.characterBody.bestFitRadius
				}, transmit: true);
			}
			if ((bool)cachedModelTransform)
			{
				EntityState.Destroy(cachedModelTransform.gameObject);
				cachedModelTransform = null;
			}
		}
		if (isPlayerDeath && (bool)base.characterBody)
		{
			Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/TemporaryVisualEffects/PlayerDeathEffect"), base.characterBody.corePosition, Quaternion.identity).GetComponent<LocalCameraEffect>().targetCharacter = base.characterBody.gameObject;
		}
		if ((bool)cachedModelTransform)
		{
			if (isBrittle)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(cachedModelTransform.gameObject);
				temporaryOverlayInstance.duration = 0.5f;
				temporaryOverlayInstance.destroyObjectOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matShatteredGlass");
				temporaryOverlayInstance.destroyEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BrittleDeath");
				temporaryOverlayInstance.destroyEffectChildString = "Chest";
				temporaryOverlayInstance.inspectorCharacterModel = cachedModelTransform.gameObject.GetComponent<CharacterModel>();
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.transmit = false;
			}
			if ((bool)base.cameraTargetParams)
			{
				ChildLocator component = cachedModelTransform.GetComponent<ChildLocator>();
				if ((bool)component)
				{
					Transform transform = component.FindChild("Chest");
					if ((bool)transform)
					{
						base.cameraTargetParams.cameraPivotTransform = transform;
						aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
						base.cameraTargetParams.dontRaycastToPivot = true;
					}
				}
			}
		}
		if (!isVoidDeath)
		{
			PlayDeathSound();
			PlayDeathAnimation();
			CreateDeathEffects();
		}
	}

	protected virtual void PlayDeathSound()
	{
		if ((bool)base.sfxLocator && base.sfxLocator.deathSound != "")
		{
			Util.PlaySound(base.sfxLocator.deathSound, base.gameObject);
		}
	}

	protected virtual void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.CrossFadeInFixedTime("Death", crossfadeDuration);
		}
	}

	protected virtual void CreateDeathEffects()
	{
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!NetworkServer.active)
		{
			return;
		}
		bool flag = false;
		bool flag2 = true;
		if ((bool)base.characterMotor)
		{
			flag = base.characterMotor.isGrounded;
			flag2 = base.characterMotor.atRest;
		}
		else if ((bool)base.rigidbodyMotor)
		{
			flag = false;
			flag2 = false;
		}
		float deltaTime = GetDeltaTime();
		fallingStopwatch = (flag ? 0f : (fallingStopwatch + deltaTime));
		restStopwatch = ((!flag2) ? 0f : (restStopwatch + deltaTime));
		if (!(base.fixedAge >= minTimeToKeepBodyForNetworkMessages))
		{
			return;
		}
		if (!bodyMarkedForDestructionServer)
		{
			if ((restStopwatch >= bodyPreservationDuration || fallingStopwatch >= maxFallDuration || base.fixedAge > hardCutoffDuration) && shouldAutoDestroy)
			{
				DestroyBodyAsapServer();
			}
		}
		else
		{
			OnPreDestroyBodyServer();
			EntityState.Destroy(base.gameObject);
		}
	}

	protected void DestroyBodyAsapServer()
	{
		bodyMarkedForDestructionServer = true;
	}

	protected virtual void OnPreDestroyBodyServer()
	{
	}

	protected void DestroyModel()
	{
		if ((bool)cachedModelTransform)
		{
			EntityState.Destroy(cachedModelTransform.gameObject);
			cachedModelTransform = null;
		}
	}

	public override void OnExit()
	{
		aimRequest?.Dispose();
		if (shouldAutoDestroy && fallingStopwatch >= maxFallDuration)
		{
			DestroyModel();
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
