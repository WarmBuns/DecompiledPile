using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor;

public class CorruptionTransitionBase : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationGroundStateName;

	[SerializeField]
	public string animationAirStateName;

	[SerializeField]
	public string animationPlaybackParameterName;

	[SerializeField]
	public float animationCrossfadeDuration;

	[SerializeField]
	public string entrySound;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public GameObject completionEffectPrefab;

	[SerializeField]
	public string effectmuzzle;

	[SerializeField]
	public CharacterCameraParams cameraParams;

	[SerializeField]
	public float dampingCoefficient;

	protected VoidSurvivorController voidSurvivorController;

	private GameObject chargeEffectInstance;

	private CameraTargetParams.CameraParamsOverrideHandle cameraParamsOverrideHandle;

	public override void OnEnter()
	{
		base.OnEnter();
		voidSurvivorController = GetComponent<VoidSurvivorController>();
		PlayCrossfade(animationLayerName, base.characterMotor.isGrounded ? animationGroundStateName : animationAirStateName, animationPlaybackParameterName, duration, animationCrossfadeDuration);
		Util.PlaySound(entrySound, base.gameObject);
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
		StartCameraParamsOverride(duration);
		Transform transform = FindModelChild(effectmuzzle);
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeEffectInstance.transform.parent = transform;
			ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
		if (base.isAuthority && (bool)voidSurvivorController)
		{
			voidSurvivorController.weaponStateMachine.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		EndCameraParamsOverride(0f);
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
		if ((bool)chargeEffectInstance)
		{
			EntityState.Destroy(chargeEffectInstance);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			base.characterMotor.velocity -= base.characterMotor.velocity * dampingCoefficient;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			OnFinishAuthority();
			outer.SetNextStateToMain();
		}
	}

	public virtual void OnFinishAuthority()
	{
		EffectManager.SimpleMuzzleFlash(completionEffectPrefab, base.gameObject, effectmuzzle, transmit: true);
	}

	protected void StartCameraParamsOverride(float transitionDuration)
	{
		if (!cameraParamsOverrideHandle.isValid)
		{
			cameraParamsOverrideHandle = base.cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
			{
				cameraParamsData = cameraParams.data
			}, transitionDuration);
		}
	}

	protected void EndCameraParamsOverride(float transitionDuration)
	{
		if (cameraParamsOverrideHandle.isValid)
		{
			base.cameraTargetParams.RemoveParamsOverride(cameraParamsOverrideHandle, transitionDuration);
			cameraParamsOverrideHandle = default(CameraTargetParams.CameraParamsOverrideHandle);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
