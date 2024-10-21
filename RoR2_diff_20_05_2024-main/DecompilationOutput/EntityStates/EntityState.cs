using System;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates;

public class EntityState
{
	public EntityStateMachine outer;

	protected float age { get; set; }

	protected float fixedAge { get; set; }

	protected GameObject gameObject => outer.gameObject;

	protected bool isLocalPlayer
	{
		get
		{
			if ((bool)outer.networker)
			{
				return outer.networker.isLocalPlayer;
			}
			return false;
		}
	}

	protected bool localPlayerAuthority
	{
		get
		{
			if ((bool)outer.networker)
			{
				return outer.networker.localPlayerAuthority;
			}
			return false;
		}
	}

	protected bool isAuthority => Util.HasEffectiveAuthority(outer.networkIdentity);

	protected Transform transform => outer.commonComponents.transform;

	protected CharacterBody characterBody => outer.commonComponents.characterBody;

	protected CharacterMotor characterMotor => outer.commonComponents.characterMotor;

	protected CharacterDirection characterDirection => outer.commonComponents.characterDirection;

	protected Rigidbody rigidbody => outer.commonComponents.rigidbody;

	protected RigidbodyMotor rigidbodyMotor => outer.commonComponents.rigidbodyMotor;

	protected RigidbodyDirection rigidbodyDirection => outer.commonComponents.rigidbodyDirection;

	protected RailMotor railMotor => outer.commonComponents.railMotor;

	protected ModelLocator modelLocator => outer.commonComponents.modelLocator;

	protected InputBankTest inputBank => outer.commonComponents.inputBank;

	protected TeamComponent teamComponent => outer.commonComponents.teamComponent;

	protected HealthComponent healthComponent => outer.commonComponents.healthComponent;

	protected SkillLocator skillLocator => outer.commonComponents.skillLocator;

	protected CharacterEmoteDefinitions characterEmoteDefinitions => outer.commonComponents.characterEmoteDefinitions;

	protected CameraTargetParams cameraTargetParams => outer.commonComponents.cameraTargetParams;

	protected SfxLocator sfxLocator => outer.commonComponents.sfxLocator;

	protected BodyAnimatorSmoothingParameters bodyAnimatorSmoothingParameters => outer.commonComponents.bodyAnimatorSmoothingParameters;

	protected ProjectileController projectileController => outer.commonComponents.projectileController;

	public void ClearEntityStateMachine()
	{
		outer = null;
	}

	public EntityState()
	{
		EntityStateCatalog.InitializeStateFields(this);
	}

	public virtual void OnEnter()
	{
	}

	public virtual void OnExit()
	{
	}

	public virtual void ModifyNextState(EntityState nextState)
	{
	}

	public virtual void Update()
	{
		age += Time.deltaTime;
	}

	public virtual void FixedUpdate()
	{
		fixedAge += GetDeltaTime();
	}

	protected float GetDeltaTime()
	{
		return Time.fixedDeltaTime;
	}

	public virtual void OnSerialize(NetworkWriter writer)
	{
	}

	public virtual void OnDeserialize(NetworkReader reader)
	{
	}

	public virtual InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}

	public virtual void Reset()
	{
		ClearEntityStateMachine();
		age = 0f;
		fixedAge = 0f;
	}

	public void SetAIUpdateFrequency(bool alwaysUpdate)
	{
		if ((bool)characterBody && (bool)characterBody.master)
		{
			characterBody.master.SetAIUpdateFrequency(alwaysUpdate);
		}
	}

	protected static void Destroy(UnityEngine.Object obj)
	{
		UnityEngine.Object.Destroy(obj);
	}

	protected T GetComponent<T>() where T : Component
	{
		return outer.GetComponent<T>();
	}

	protected Component GetComponent(Type type)
	{
		return outer.GetComponent(type);
	}

	protected Component GetComponent(string type)
	{
		return outer.GetComponent(type);
	}

	protected Transform GetModelBaseTransform()
	{
		if (!modelLocator)
		{
			return null;
		}
		return modelLocator.modelBaseTransform;
	}

	protected Transform GetModelTransform()
	{
		if (!modelLocator)
		{
			return null;
		}
		return modelLocator.modelTransform;
	}

	protected AimAnimator GetAimAnimator()
	{
		if ((bool)modelLocator && (bool)modelLocator.modelTransform)
		{
			return modelLocator.modelTransform.GetComponent<AimAnimator>();
		}
		return null;
	}

	protected Animator GetModelAnimator()
	{
		if ((bool)modelLocator && (bool)modelLocator.modelTransform)
		{
			return modelLocator.modelTransform.GetComponent<Animator>();
		}
		return null;
	}

	protected ChildLocator GetModelChildLocator()
	{
		if ((bool)modelLocator && (bool)modelLocator.modelTransform)
		{
			return modelLocator.modelTransform.GetComponent<ChildLocator>();
		}
		return null;
	}

	protected RootMotionAccumulator GetModelRootMotionAccumulator()
	{
		if ((bool)modelLocator && (bool)modelLocator.modelTransform)
		{
			return modelLocator.modelTransform.GetComponent<RootMotionAccumulator>();
		}
		return null;
	}

	protected void PlayAnimation(string layerName, string animationStateName, string playbackRateParam, float duration, float transition = 0f)
	{
		if (duration <= 0f)
		{
			Debug.LogWarningFormat("EntityState.PlayAnimation: Zero duration is not allowed. type={0}", GetType().Name);
			return;
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayAnimationOnAnimator(modelAnimator, layerName, animationStateName, playbackRateParam, duration, transition);
		}
	}

	protected void PlayAnimation(string layerName, int animationStateHash, int playbackRateParamHash, float duration)
	{
		if (duration <= 0f)
		{
			Debug.LogWarningFormat("EntityState.PlayAnimation: Zero duration is not allowed. type={0}", GetType().Name);
			return;
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex(layerName);
			if (layerIndex >= 0)
			{
				PlayAnimationOnAnimator(modelAnimator, layerIndex, animationStateHash, playbackRateParamHash, duration);
			}
		}
	}

	protected static void PlayAnimationOnAnimator(Animator modelAnimator, string layerName, string animationStateName, string playbackRateParam, float duration, float transition = 0f)
	{
		modelAnimator.speed = 1f;
		modelAnimator.Update(0f);
		int layerIndex = modelAnimator.GetLayerIndex(layerName);
		if (layerIndex >= 0)
		{
			if (!string.IsNullOrEmpty(playbackRateParam))
			{
				modelAnimator.SetFloat(playbackRateParam, 1f);
			}
			if (transition > 0f)
			{
				modelAnimator.CrossFadeInFixedTime(animationStateName, transition, layerIndex);
			}
			else
			{
				modelAnimator.PlayInFixedTime(animationStateName, layerIndex, 0f);
			}
			modelAnimator.Update(0f);
			float length = modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).length;
			if (!string.IsNullOrEmpty(playbackRateParam))
			{
				modelAnimator.SetFloat(playbackRateParam, length / duration);
			}
		}
	}

	protected static void PlayAnimationOnAnimator(Animator modelAnimator, string layerName, int animationStateHash, int playbackRateParamHash, float duration)
	{
		int layerIndex = modelAnimator.GetLayerIndex(layerName);
		if (layerIndex >= 0)
		{
			PlayAnimationOnAnimator(modelAnimator, layerIndex, animationStateHash, playbackRateParamHash, duration);
		}
	}

	protected static void PlayAnimationOnAnimator(Animator modelAnimator, int layerIndex, int animationStateHash, int playbackRateParamHash, float duration)
	{
		modelAnimator.speed = 1f;
		modelAnimator.Update(0f);
		if (layerIndex >= 0)
		{
			modelAnimator.SetFloat(playbackRateParamHash, 1f);
			modelAnimator.PlayInFixedTime(animationStateHash, layerIndex, 0f);
			modelAnimator.Update(0f);
			float length = modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).length;
			modelAnimator.SetFloat(playbackRateParamHash, length / duration);
		}
	}

	protected void PlayCrossfade(string layerName, string animationStateName, string playbackRateParam, float duration, float crossfadeDuration)
	{
		if (duration <= 0f)
		{
			Debug.LogWarningFormat("EntityState.PlayCrossfade: Zero duration is not allowed. type={0}", GetType().Name);
			return;
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.speed = 1f;
			modelAnimator.Update(0f);
			int layerIndex = modelAnimator.GetLayerIndex(layerName);
			modelAnimator.SetFloat(playbackRateParam, 1f);
			modelAnimator.CrossFadeInFixedTime(animationStateName, crossfadeDuration, layerIndex);
			modelAnimator.Update(0f);
			float length = modelAnimator.GetNextAnimatorStateInfo(layerIndex).length;
			modelAnimator.SetFloat(playbackRateParam, length / duration);
		}
	}

	protected void PlayCrossfade(string layerName, int animationStateNameHash, int playbackRateParamHash, float duration, float crossfadeDuration)
	{
		if (duration <= 0f)
		{
			Debug.LogWarningFormat("EntityState.PlayCrossfade: Zero duration is not allowed. type={0}", GetType().Name);
			return;
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.speed = 1f;
			modelAnimator.Update(0f);
			int layerIndex = modelAnimator.GetLayerIndex(layerName);
			modelAnimator.SetFloat(playbackRateParamHash, 1f);
			modelAnimator.CrossFadeInFixedTime(animationStateNameHash, crossfadeDuration, layerIndex);
			modelAnimator.Update(0f);
			float length = modelAnimator.GetNextAnimatorStateInfo(layerIndex).length;
			modelAnimator.SetFloat(playbackRateParamHash, length / duration);
		}
	}

	protected void PlayCrossfade(string layerName, string animationStateName, float crossfadeDuration)
	{
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.speed = 1f;
			modelAnimator.Update(0f);
			int layerIndex = modelAnimator.GetLayerIndex(layerName);
			modelAnimator.CrossFadeInFixedTime(animationStateName, crossfadeDuration, layerIndex);
		}
	}

	protected void PlayCrossfade(string layerName, int animationStateHash, float crossfadeDuration)
	{
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.speed = 1f;
			modelAnimator.Update(0f);
			int layerIndex = modelAnimator.GetLayerIndex(layerName);
			modelAnimator.CrossFadeInFixedTime(animationStateHash, crossfadeDuration, layerIndex);
		}
	}

	public virtual void PlayAnimation(string layerName, string animationStateName)
	{
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayAnimationOnAnimator(modelAnimator, layerName, animationStateName);
		}
	}

	public virtual void PlayAnimation(string layerName, int animationStateHash)
	{
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayAnimationOnAnimator(modelAnimator, layerName, animationStateHash);
		}
	}

	public virtual void SetPingable(bool value)
	{
		if ((bool)outer && (bool)outer.networkIdentity)
		{
			outer.networkIdentity.isPingable = value;
		}
	}

	protected static void PlayAnimationOnAnimator(Animator modelAnimator, string layerName, string animationStateName)
	{
		int layerIndex = modelAnimator.GetLayerIndex(layerName);
		modelAnimator.speed = 1f;
		modelAnimator.Update(0f);
		modelAnimator.PlayInFixedTime(animationStateName, layerIndex, 0f);
	}

	protected static void PlayAnimationOnAnimator(Animator modelAnimator, string layerName, int animationStateHash)
	{
		int layerIndex = modelAnimator.GetLayerIndex(layerName);
		modelAnimator.speed = 1f;
		modelAnimator.Update(0f);
		modelAnimator.PlayInFixedTime(animationStateHash, layerIndex, 0f);
	}

	protected void GetBodyAnimatorSmoothingParameters(out BodyAnimatorSmoothingParameters.SmoothingParameters smoothingParameters)
	{
		if ((bool)bodyAnimatorSmoothingParameters)
		{
			smoothingParameters = bodyAnimatorSmoothingParameters.smoothingParameters;
		}
		else
		{
			smoothingParameters = BodyAnimatorSmoothingParameters.defaultParameters;
		}
	}
}
