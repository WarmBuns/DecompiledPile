using System;
using System.Collections.Generic;
using EntityStates;
using HG;
using JetBrains.Annotations;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class EntityStateMachine : MonoBehaviour, IManagedMonoBehaviour
{
	public struct CommonComponentCache
	{
		public readonly Transform transform;

		public readonly CharacterBody characterBody;

		public readonly CharacterMotor characterMotor;

		public readonly CharacterDirection characterDirection;

		public readonly Rigidbody rigidbody;

		public readonly RigidbodyMotor rigidbodyMotor;

		public readonly RigidbodyDirection rigidbodyDirection;

		public readonly RailMotor railMotor;

		public readonly ModelLocator modelLocator;

		public readonly InputBankTest inputBank;

		public readonly TeamComponent teamComponent;

		public readonly HealthComponent healthComponent;

		public readonly SkillLocator skillLocator;

		public readonly CharacterEmoteDefinitions characterEmoteDefinitions;

		public readonly CameraTargetParams cameraTargetParams;

		public readonly SfxLocator sfxLocator;

		public readonly BodyAnimatorSmoothingParameters bodyAnimatorSmoothingParameters;

		public readonly ProjectileController projectileController;

		public CommonComponentCache(GameObject gameObject)
		{
			transform = gameObject.transform;
			characterBody = gameObject.GetComponent<CharacterBody>();
			characterMotor = gameObject.GetComponent<CharacterMotor>();
			characterDirection = gameObject.GetComponent<CharacterDirection>();
			rigidbody = gameObject.GetComponent<Rigidbody>();
			rigidbodyMotor = gameObject.GetComponent<RigidbodyMotor>();
			rigidbodyDirection = gameObject.GetComponent<RigidbodyDirection>();
			railMotor = gameObject.GetComponent<RailMotor>();
			modelLocator = gameObject.GetComponent<ModelLocator>();
			inputBank = gameObject.GetComponent<InputBankTest>();
			teamComponent = gameObject.GetComponent<TeamComponent>();
			healthComponent = gameObject.GetComponent<HealthComponent>();
			skillLocator = gameObject.GetComponent<SkillLocator>();
			characterEmoteDefinitions = gameObject.GetComponent<CharacterEmoteDefinitions>();
			cameraTargetParams = gameObject.GetComponent<CameraTargetParams>();
			sfxLocator = gameObject.GetComponent<SfxLocator>();
			bodyAnimatorSmoothingParameters = gameObject.GetComponent<BodyAnimatorSmoothingParameters>();
			projectileController = gameObject.GetComponent<ProjectileController>();
		}
	}

	public delegate void ModifyNextStateDelegate(EntityStateMachine entityStateMachine, ref EntityState newNextState);

	private EntityState nextState;

	[Tooltip("The name of this state machine.")]
	public string customName;

	[Tooltip("The type of the state to enter when this component is first activated.")]
	public SerializableEntityStateType initialStateType = new SerializableEntityStateType(typeof(TestState1));

	[Tooltip("The preferred main state of this state machine.")]
	public SerializableEntityStateType mainStateType;

	public CommonComponentCache commonComponents;

	[NonSerialized]
	public int networkIndex = -1;

	[NonSerialized]
	public bool AllowStartWithoutNetworker;

	[NonSerialized]
	public bool ShouldStateTransitionOnUpdate;

	public ModifyNextStateDelegate nextStateModifier;

	public bool debug;

	public EntityState state { get; private set; }

	public NetworkStateMachine networker { get; set; }

	public NetworkIdentity networkIdentity { get; set; }

	public bool AlwaysUpdate => false;

	public bool destroying { get; private set; }

	public void SetNextState(EntityState newNextState)
	{
		nextStateModifier?.Invoke(this, ref newNextState);
		nextState = newNextState;
	}

	public void SetNextStateToMain()
	{
		SetNextState(EntityStateCatalog.InstantiateState(ref mainStateType));
	}

	public bool CanInterruptState(InterruptPriority interruptPriority)
	{
		return (nextState ?? state).GetMinimumInterruptPriority() <= interruptPriority;
	}

	public bool SetInterruptState(EntityState newNextState, InterruptPriority interruptPriority)
	{
		if (CanInterruptState(interruptPriority))
		{
			_ = debug;
			SetNextState(newNextState);
			return true;
		}
		return false;
	}

	public bool HasPendingState()
	{
		return nextState != null;
	}

	public bool IsInMainState()
	{
		if (state != null)
		{
			return state.GetType() == mainStateType.stateType;
		}
		return false;
	}

	public bool IsInInitialState()
	{
		if (state != null)
		{
			return state.GetType() == initialStateType.stateType;
		}
		return false;
	}

	public void SetState([NotNull] EntityState newState)
	{
		nextState = null;
		newState.outer = this;
		if (state == null)
		{
			Debug.LogErrorFormat("State machine {0} on object {1} does not have a state!", customName, base.gameObject);
		}
		state.ModifyNextState(newState);
		state.OnExit();
		_ = debug;
		state = newState;
		state.OnEnter();
		if (networkIndex != -1)
		{
			if (!networker)
			{
				Debug.LogErrorFormat("State machine {0} on object {1} does not have a networker assigned!", customName, base.gameObject);
			}
			networker.SendSetEntityState(networkIndex);
		}
	}

	private void Awake()
	{
		if (!networker)
		{
			networker = GetComponent<NetworkStateMachine>();
		}
		if (!networkIdentity)
		{
			networkIdentity = GetComponent<NetworkIdentity>();
		}
		commonComponents = new CommonComponentCache(base.gameObject);
		state = new Uninitialized();
		state.outer = this;
	}

	private void Start()
	{
		if (nextState != null && (AllowStartWithoutNetworker || ((bool)networker && !networker.hasAuthority)))
		{
			SetState(nextState);
			return;
		}
		Type stateType = initialStateType.stateType;
		if (state is Uninitialized && stateType != null && stateType.IsSubclassOf(typeof(EntityState)))
		{
			SetState(EntityStateCatalog.InstantiateState(stateType));
		}
	}

	public void Update()
	{
		ManagedUpdate();
	}

	public void FixedUpdate()
	{
		ManagedFixedUpdate(Time.deltaTime);
	}

	public void ManagedUpdate()
	{
		state.Update();
		if (ShouldStateTransitionOnUpdate)
		{
			TryStateTransition();
		}
	}

	public void ManagedFixedUpdate(float deltaTime)
	{
		if (!destroying)
		{
			if (!ShouldStateTransitionOnUpdate)
			{
				TryStateTransition();
			}
			state.FixedUpdate();
		}
	}

	private void TryStateTransition()
	{
		if (nextState != null)
		{
			SetState(nextState);
		}
	}

	private void OnDestroy()
	{
		destroying = true;
		if (state != null)
		{
			state.OnExit();
			state = null;
		}
	}

	private void OnValidate()
	{
		if (!(mainStateType.stateType == null))
		{
			return;
		}
		if (customName == "Body")
		{
			if ((bool)GetComponent<CharacterMotor>())
			{
				mainStateType = new SerializableEntityStateType(typeof(GenericCharacterMain));
			}
			else if ((bool)GetComponent<RigidbodyMotor>())
			{
				mainStateType = new SerializableEntityStateType(typeof(FlyState));
			}
		}
		else if (customName == "Weapon")
		{
			mainStateType = new SerializableEntityStateType(typeof(Idle));
		}
		else if (customName == "AI")
		{
			BaseAI component = GetComponent<BaseAI>();
			if ((bool)component)
			{
				mainStateType = component.scanState;
			}
		}
	}

	public static EntityStateMachine FindByCustomName(GameObject gameObject, string customName)
	{
		List<EntityStateMachine> gameObjectComponents = GetComponentsCache<EntityStateMachine>.GetGameObjectComponents(gameObject);
		EntityStateMachine result = null;
		int i = 0;
		for (int count = gameObjectComponents.Count; i < count; i++)
		{
			if (string.CompareOrdinal(customName, gameObjectComponents[i].customName) == 0)
			{
				result = gameObjectComponents[i];
				break;
			}
		}
		GetComponentsCache<EntityStateMachine>.ReturnBuffer(gameObjectComponents);
		return result;
	}
}
