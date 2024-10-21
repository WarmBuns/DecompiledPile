using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using EntityStates;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

[DisallowMultipleComponent]
public class VehicleSeat : NetworkBehaviour, IInteractable
{
	private struct PassengerInfo
	{
		private static readonly List<EntityStateMachine> sharedBuffer = new List<EntityStateMachine>();

		public readonly Transform transform;

		public readonly CharacterBody body;

		public readonly InputBankTest inputBank;

		public readonly InteractionDriver interactionDriver;

		public readonly CharacterMotor characterMotor;

		public readonly EntityStateMachine bodyStateMachine;

		public readonly CharacterModel characterModel;

		public readonly NetworkIdentity networkIdentity;

		public readonly Collider collider;

		public bool hasEffectiveAuthority => Util.HasEffectiveAuthority(networkIdentity);

		public PassengerInfo(GameObject passengerBodyObject)
		{
			transform = passengerBodyObject.transform;
			body = passengerBodyObject.GetComponent<CharacterBody>();
			inputBank = passengerBodyObject.GetComponent<InputBankTest>();
			interactionDriver = passengerBodyObject.GetComponent<InteractionDriver>();
			characterMotor = passengerBodyObject.GetComponent<CharacterMotor>();
			networkIdentity = passengerBodyObject.GetComponent<NetworkIdentity>();
			collider = passengerBodyObject.GetComponent<Collider>();
			bodyStateMachine = null;
			passengerBodyObject.GetComponents(sharedBuffer);
			for (int i = 0; i < sharedBuffer.Count; i++)
			{
				EntityStateMachine entityStateMachine = sharedBuffer[i];
				if (string.CompareOrdinal(entityStateMachine.customName, "Body") == 0)
				{
					bodyStateMachine = entityStateMachine;
					break;
				}
			}
			sharedBuffer.Clear();
			characterModel = null;
			if ((bool)body.modelLocator && (bool)body.modelLocator.modelTransform)
			{
				characterModel = body.modelLocator.modelTransform.GetComponent<CharacterModel>();
			}
		}

		public string GetString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("transform=").Append(transform).AppendLine();
			stringBuilder.Append("body=").Append(body).AppendLine();
			stringBuilder.Append("inputBank=").Append(inputBank).AppendLine();
			stringBuilder.Append("interactionDriver=").Append(interactionDriver).AppendLine();
			stringBuilder.Append("characterMotor=").Append(characterMotor).AppendLine();
			stringBuilder.Append("bodyStateMachine=").Append(bodyStateMachine).AppendLine();
			stringBuilder.Append("characterModel=").Append(characterModel).AppendLine();
			stringBuilder.Append("networkIdentity=").Append(networkIdentity).AppendLine();
			stringBuilder.Append("hasEffectiveAuthority=").Append(hasEffectiveAuthority).AppendLine();
			return stringBuilder.ToString();
		}
	}

	public delegate void InteractabilityCheckDelegate(CharacterBody activator, ref Interactability? resultOverride);

	public SerializableEntityStateType passengerState;

	public Transform seatPosition;

	public Transform exitPosition;

	public bool ejectOnCollision;

	public bool hidePassenger = true;

	public float exitVelocityFraction = 1f;

	public UnityEvent onPassengerEnterUnityEvent;

	[FormerlySerializedAs("OnPassengerExitUnityEvent")]
	public UnityEvent onPassengerExitUnityEvent;

	public string enterVehicleContextString;

	public string exitVehicleContextString;

	public bool disablePassengerMotor;

	public bool isEquipmentActivationAllowed;

	public bool shouldProximityHighlight = true;

	[SyncVar(hook = "SetPassenger")]
	private GameObject passengerBodyObject;

	private PassengerInfo passengerInfo;

	private Rigidbody rigidbody;

	private Collider collider;

	[HideInInspector]
	public CameraTargetParams targetParams;

	public CallbackCheck<Interactability, CharacterBody> enterVehicleAllowedCheck = new CallbackCheck<Interactability, CharacterBody>();

	public CallbackCheck<Interactability, CharacterBody> exitVehicleAllowedCheck = new CallbackCheck<Interactability, CharacterBody>();

	public CallbackCheck<bool, GameObject> handleVehicleEnterRequestServer = new CallbackCheck<bool, GameObject>();

	public CallbackCheck<bool, GameObject> handleVehicleExitRequestServer = new CallbackCheck<bool, GameObject>();

	private static readonly BoolConVar cvVehicleSeatDebug;

	private Run.FixedTimeStamp passengerAssignmentTime = Run.FixedTimeStamp.negativeInfinity;

	private readonly float passengerAssignmentCooldown = 0.2f;

	private NetworkInstanceId ___passengerBodyObjectNetId;

	private static int kCmdCmdEjectPassenger;

	public CharacterBody currentPassengerBody => passengerInfo.body;

	public InputBankTest currentPassengerInputBank => passengerInfo.inputBank;

	private static bool shouldLog => cvVehicleSeatDebug.value;

	public bool hasPassenger => passengerBodyObject;

	public GameObject NetworkpassengerBodyObject
	{
		get
		{
			return passengerBodyObject;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetPassenger(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVarGameObject(value, ref passengerBodyObject, 1u, ref ___passengerBodyObjectNetId);
		}
	}

	public event Action<GameObject> onPassengerEnter;

	public event Action<GameObject> onPassengerExit;

	public static event Action<VehicleSeat, GameObject> onPassengerEnterGlobal;

	public static event Action<VehicleSeat, GameObject> onPassengerExitGlobal;

	public string GetContextString(Interactor activator)
	{
		if (!passengerBodyObject)
		{
			return Language.GetString(enterVehicleContextString);
		}
		if ((object)passengerBodyObject == activator.gameObject)
		{
			return Language.GetString(exitVehicleContextString);
		}
		return null;
	}

	public Interactability GetInteractability(Interactor activator)
	{
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if (!passengerBodyObject)
		{
			return enterVehicleAllowedCheck.Evaluate(component) ?? Interactability.Available;
		}
		if ((object)passengerBodyObject == activator.gameObject && passengerAssignmentTime.timeSince >= passengerAssignmentCooldown)
		{
			return exitVehicleAllowedCheck.Evaluate(component) ?? Interactability.Available;
		}
		if ((bool)component && (bool)component.currentVehicle && (object)component.currentVehicle != this)
		{
			return Interactability.ConditionsNotMet;
		}
		return Interactability.Disabled;
	}

	public void OnInteractionBegin(Interactor activator)
	{
		if (!passengerBodyObject)
		{
			if (!handleVehicleEnterRequestServer.Evaluate(activator.gameObject).HasValue)
			{
				SetPassenger(activator.gameObject);
			}
		}
		else if ((object)activator.gameObject == passengerBodyObject && !handleVehicleExitRequestServer.Evaluate(activator.gameObject).HasValue)
		{
			SetPassenger(null);
		}
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public bool ShouldShowOnScanner()
	{
		return true;
	}

	public bool ShouldProximityHighlight()
	{
		return shouldProximityHighlight;
	}

	private void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		collider = GetComponent<Collider>();
		targetParams = GetComponent<CameraTargetParams>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (!NetworkServer.active && (bool)passengerBodyObject)
		{
			OnPassengerEnter(passengerBodyObject);
		}
	}

	private void SetPassengerInternal(GameObject newPassengerBodyObject)
	{
		if ((bool)passengerBodyObject)
		{
			OnPassengerExit(passengerBodyObject);
		}
		NetworkpassengerBodyObject = newPassengerBodyObject;
		passengerInfo = default(PassengerInfo);
		passengerAssignmentTime = Run.FixedTimeStamp.now;
		if ((bool)passengerBodyObject)
		{
			OnPassengerEnter(passengerBodyObject);
		}
		_ = shouldLog;
	}

	private void SetPassenger(GameObject newPassengerBodyObject)
	{
		string text = (newPassengerBodyObject ? Util.GetBestBodyName(newPassengerBodyObject) : "null");
		if (shouldLog)
		{
			Debug.LogFormat("SetPassenger passenger={0}", text);
		}
		if (base.syncVarHookGuard)
		{
			_ = shouldLog;
			NetworkpassengerBodyObject = newPassengerBodyObject;
			return;
		}
		_ = shouldLog;
		if ((object)passengerBodyObject == newPassengerBodyObject)
		{
			_ = shouldLog;
			return;
		}
		_ = shouldLog;
		SetPassengerInternal(newPassengerBodyObject);
	}

	private void OnPassengerMovementHit(ref CharacterMotor.MovementHitInfo movementHitInfo)
	{
		if (NetworkServer.active && ejectOnCollision && passengerAssignmentTime.timeSince > Time.fixedDeltaTime)
		{
			SetPassenger(null);
		}
	}

	private void ForcePassengerState()
	{
		if ((bool)passengerInfo.bodyStateMachine && passengerInfo.hasEffectiveAuthority)
		{
			Type type = passengerState.GetType();
			if (passengerInfo.bodyStateMachine.state.GetType() != type)
			{
				passengerInfo.bodyStateMachine.SetInterruptState(EntityStateCatalog.InstantiateState(ref passengerState), InterruptPriority.Vehicle);
			}
		}
	}

	private void FixedUpdate()
	{
		ForcePassengerState();
		UpdatePassengerPosition();
		if ((bool)passengerInfo.characterMotor)
		{
			passengerInfo.characterMotor.velocity = Vector3.zero;
		}
	}

	private void UpdatePassengerPosition()
	{
		Vector3 position = seatPosition.position;
		if ((bool)passengerInfo.characterMotor)
		{
			passengerInfo.characterMotor.velocity = Vector3.zero;
			passengerInfo.characterMotor.Motor.BaseVelocity = Vector3.zero;
			passengerInfo.characterMotor.Motor.SetPosition(position);
			if (!disablePassengerMotor && Time.inFixedTimeStep)
			{
				passengerInfo.characterMotor.rootMotion = position - passengerInfo.transform.position;
			}
		}
		else if ((bool)passengerInfo.transform)
		{
			passengerInfo.transform.position = position;
		}
	}

	[Server]
	public bool AssignPassenger(GameObject bodyObject)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.VehicleSeat::AssignPassenger(UnityEngine.GameObject)' called on client");
			return false;
		}
		if ((bool)passengerBodyObject)
		{
			return false;
		}
		if ((bool)bodyObject)
		{
			CharacterBody component = bodyObject.GetComponent<CharacterBody>();
			if ((bool)component && (bool)component.currentVehicle)
			{
				component.currentVehicle.EjectPassenger(bodyObject);
			}
		}
		SetPassenger(bodyObject);
		return true;
	}

	[Server]
	public void EjectPassenger(GameObject bodyObject)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VehicleSeat::EjectPassenger(UnityEngine.GameObject)' called on client");
		}
		else if ((object)bodyObject == passengerBodyObject)
		{
			SetPassenger(null);
		}
	}

	[Server]
	public void EjectPassenger()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VehicleSeat::EjectPassenger()' called on client");
		}
		else
		{
			SetPassenger(null);
		}
	}

	[Command]
	public void CmdEjectPassenger()
	{
		SetPassenger(null);
	}

	private void OnDestroy()
	{
		SetPassenger(null);
	}

	private void OnPassengerEnter(GameObject passenger)
	{
		passengerInfo = new PassengerInfo(passengerBodyObject);
		if ((bool)passengerInfo.body)
		{
			passengerInfo.body.currentVehicle = this;
		}
		if (hidePassenger && (bool)passengerInfo.characterModel)
		{
			passengerInfo.characterModel.invisibilityCount++;
		}
		ForcePassengerState();
		if ((bool)passengerInfo.characterMotor)
		{
			if (disablePassengerMotor)
			{
				passengerInfo.characterMotor.enabled = false;
			}
			else
			{
				passengerInfo.characterMotor.onMovementHit += OnPassengerMovementHit;
			}
		}
		if ((bool)passengerInfo.collider && (bool)collider)
		{
			Physics.IgnoreCollision(collider, passengerInfo.collider, ignore: true);
		}
		if ((bool)passengerInfo.interactionDriver)
		{
			passengerInfo.interactionDriver.interactableOverride = base.gameObject;
		}
		_ = shouldLog;
		this.onPassengerEnter?.Invoke(passengerBodyObject);
		onPassengerEnterUnityEvent?.Invoke();
		VehicleSeat.onPassengerEnterGlobal?.Invoke(this, passengerBodyObject);
	}

	private void OnPassengerExit(GameObject passenger)
	{
		_ = shouldLog;
		if (hidePassenger && (bool)passengerInfo.characterModel)
		{
			passengerInfo.characterModel.invisibilityCount--;
		}
		if ((bool)passengerInfo.body)
		{
			passengerInfo.body.currentVehicle = null;
		}
		if ((bool)passengerInfo.characterMotor)
		{
			if (disablePassengerMotor)
			{
				passengerInfo.characterMotor.enabled = true;
			}
			else
			{
				passengerInfo.characterMotor.onMovementHit -= OnPassengerMovementHit;
			}
			passengerInfo.characterMotor.velocity = Vector3.zero;
			passengerInfo.characterMotor.rootMotion = Vector3.zero;
			passengerInfo.characterMotor.Motor.BaseVelocity = Vector3.zero;
		}
		if ((bool)passengerInfo.collider && (bool)collider)
		{
			Physics.IgnoreCollision(collider, passengerInfo.collider, ignore: false);
		}
		if (passengerInfo.hasEffectiveAuthority)
		{
			if ((bool)passengerInfo.bodyStateMachine && passengerInfo.bodyStateMachine.CanInterruptState(InterruptPriority.Vehicle))
			{
				passengerInfo.bodyStateMachine.SetNextStateToMain();
			}
			Vector3 newPosition = (exitPosition ? exitPosition.position : seatPosition.position);
			TeleportHelper.TeleportGameObject(passengerInfo.transform.gameObject, newPosition);
		}
		if ((bool)passengerInfo.interactionDriver && (object)passengerInfo.interactionDriver.interactableOverride == base.gameObject)
		{
			passengerInfo.interactionDriver.interactableOverride = null;
		}
		if ((bool)rigidbody && (bool)passengerInfo.characterMotor)
		{
			passengerInfo.characterMotor.velocity = rigidbody.velocity * exitVelocityFraction;
		}
		this.onPassengerExit?.Invoke(passengerBodyObject);
		onPassengerExitUnityEvent?.Invoke();
		VehicleSeat.onPassengerExitGlobal?.Invoke(this, passengerBodyObject);
	}

	static VehicleSeat()
	{
		cvVehicleSeatDebug = new BoolConVar("vehicle_seat_debug", ConVarFlags.None, "0", "Enables debug logging for VehicleSeat.");
		kCmdCmdEjectPassenger = -1985903462;
		NetworkBehaviour.RegisterCommandDelegate(typeof(VehicleSeat), kCmdCmdEjectPassenger, InvokeCmdCmdEjectPassenger);
		NetworkCRC.RegisterBehaviour("VehicleSeat", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdEjectPassenger(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdEjectPassenger called on client.");
		}
		else
		{
			((VehicleSeat)obj).CmdEjectPassenger();
		}
	}

	public void CallCmdEjectPassenger()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdEjectPassenger called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdEjectPassenger();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdEjectPassenger);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdEjectPassenger");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(passengerBodyObject);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(passengerBodyObject);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			___passengerBodyObjectNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetPassenger(reader.ReadGameObject());
		}
	}

	public override void PreStartClient()
	{
		if (!___passengerBodyObjectNetId.IsEmpty())
		{
			NetworkpassengerBodyObject = ClientScene.FindLocalObject(___passengerBodyObjectNetId);
		}
	}
}
