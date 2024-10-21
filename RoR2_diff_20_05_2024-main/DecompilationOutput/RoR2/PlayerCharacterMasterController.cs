using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rewired;
using RoR2.CameraModes;
using RoR2.UI;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterMaster))]
[RequireComponent(typeof(PingerController))]
public class PlayerCharacterMasterController : NetworkBehaviour
{
	private static List<PlayerCharacterMasterController> _instances;

	private static ReadOnlyCollection<PlayerCharacterMasterController> _instancesReadOnly;

	private CharacterBody body;

	private InputBankTest bodyInputs;

	private CharacterMotor bodyMotor;

	private GameObject previousBodyObject;

	[CompilerGenerated]
	private string _003CfinalMessageTokenServer_003Ek__BackingField;

	private PingerController pingerController;

	[SyncVar(hook = "OnSyncNetworkUserInstanceId")]
	private NetworkInstanceId networkUserInstanceId;

	private GameObject resolvedNetworkUserGameObjectInstance;

	private bool networkUserResolved;

	private NetworkUser resolvedNetworkUserInstance;

	private bool alreadyLinkedToNetworkUserOnce;

	public float cameraMinPitch = -70f;

	public float cameraMaxPitch = 70f;

	public GameObject crosshair;

	public Vector3 crosshairPosition;

	private NetworkIdentity netid;

	private static readonly float sprintMinAimMoveDot;

	private bool sprintInputPressReceived;

	private bool wasClaimed;

	private float lunarCoinChanceMultiplier = 0.5f;

	private static int kRpcRpcOnLinkedToNetworkUser;

	private static int kRpcRpcIncrementRunCount;

	public static ReadOnlyCollection<PlayerCharacterMasterController> instances => _instancesReadOnly;

	private bool bodyIsFlier
	{
		get
		{
			if ((bool)bodyMotor)
			{
				return bodyMotor.isFlying;
			}
			return true;
		}
	}

	public CharacterMaster master { get; private set; }

	private NetworkIdentity networkIdentity => master.networkIdentity;

	public string finalMessageTokenServer
	{
		[CompilerGenerated]
		[Server]
		get
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.String RoR2.PlayerCharacterMasterController::get_finalMessageTokenServer()' called on client");
				return null;
			}
			return _003CfinalMessageTokenServer_003Ek__BackingField;
		}
		[CompilerGenerated]
		[Server]
		set
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.PlayerCharacterMasterController::set_finalMessageTokenServer(System.String)' called on client");
			}
			else
			{
				_003CfinalMessageTokenServer_003Ek__BackingField = value;
			}
		}
	} = string.Empty;

	public bool hasEffectiveAuthority => master.hasEffectiveAuthority;

	public GameObject networkUserObject
	{
		get
		{
			if (!networkUserResolved)
			{
				resolvedNetworkUserGameObjectInstance = Util.FindNetworkObject(networkUserInstanceId);
				resolvedNetworkUserInstance = null;
				if ((bool)resolvedNetworkUserGameObjectInstance)
				{
					resolvedNetworkUserInstance = resolvedNetworkUserGameObjectInstance.GetComponent<NetworkUser>();
					networkUserResolved = true;
					SetBodyPrefabToPreference();
				}
			}
			return resolvedNetworkUserGameObjectInstance;
		}
		private set
		{
			NetworkInstanceId invalid = NetworkInstanceId.Invalid;
			resolvedNetworkUserGameObjectInstance = null;
			resolvedNetworkUserInstance = null;
			networkUserResolved = true;
			if ((bool)value)
			{
				NetworkIdentity component = value.GetComponent<NetworkIdentity>();
				if ((bool)component)
				{
					invalid = component.netId;
					resolvedNetworkUserGameObjectInstance = value;
					resolvedNetworkUserInstance = value.GetComponent<NetworkUser>();
					SetBodyPrefabToPreference();
				}
			}
			NetworknetworkUserInstanceId = invalid;
		}
	}

	public NetworkUser networkUser
	{
		get
		{
			if (!networkUserObject)
			{
				return null;
			}
			return resolvedNetworkUserInstance;
		}
	}

	public bool isConnected => networkUserObject;

	public bool preventGameOver => master.preventGameOver;

	public NetworkInstanceId NetworknetworkUserInstanceId
	{
		get
		{
			return networkUserInstanceId;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncNetworkUserInstanceId(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref networkUserInstanceId, 1u);
		}
	}

	public event Action onLinkedToNetworkUserServer;

	public static event Action<PlayerCharacterMasterController> onPlayerAdded;

	public static event Action<PlayerCharacterMasterController> onPlayerRemoved;

	public static event Action<PlayerCharacterMasterController> onLinkedToNetworkUserServerGlobal;

	public static event Action<PlayerCharacterMasterController> onLinkedToNetworkUserLocal;

	private void OnSyncNetworkUserInstanceId(NetworkInstanceId value)
	{
		resolvedNetworkUserGameObjectInstance = null;
		resolvedNetworkUserInstance = null;
		networkUserResolved = value == NetworkInstanceId.Invalid;
		NetworknetworkUserInstanceId = value;
	}

	[Server]
	public void LinkToNetworkUserServer(NetworkUser networkUser)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PlayerCharacterMasterController::LinkToNetworkUserServer(RoR2.NetworkUser)' called on client");
			return;
		}
		networkUser.masterObject = base.gameObject;
		networkUserObject = networkUser.gameObject;
		networkIdentity.AssignClientAuthority(networkUser.connectionToClient);
		if (!alreadyLinkedToNetworkUserOnce)
		{
			networkUser.CopyLoadoutToMaster();
			alreadyLinkedToNetworkUserOnce = true;
		}
		else
		{
			networkUser.CopyLoadoutFromMaster();
		}
		SetBodyPrefabToPreference();
		this.onLinkedToNetworkUserServer?.Invoke();
		PlayerCharacterMasterController.onLinkedToNetworkUserServerGlobal?.Invoke(this);
		HandleLinkedToNetworkUser(networkUser.id);
		CallRpcOnLinkedToNetworkUser(networkUser.id);
	}

	[ClientRpc]
	private void RpcOnLinkedToNetworkUser(NetworkUserId id)
	{
		HandleLinkedToNetworkUser(id);
	}

	private void HandleLinkedToNetworkUser(NetworkUserId id)
	{
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			if ((bool)readOnlyLocalUsers.currentNetworkUser && readOnlyLocalUsers.currentNetworkUser.id.Equals(id))
			{
				PlayerCharacterMasterController.onLinkedToNetworkUserLocal?.Invoke(this);
				break;
			}
		}
	}

	private void Awake()
	{
		master = GetComponent<CharacterMaster>();
		netid = GetComponent<NetworkIdentity>();
		pingerController = GetComponent<PingerController>();
	}

	private void OnEnable()
	{
		_instances.Add(this);
		if (PlayerCharacterMasterController.onPlayerAdded != null)
		{
			PlayerCharacterMasterController.onPlayerAdded(this);
		}
		NetworkUser.onNetworkUserBodyPreferenceChanged += OnNetworkUserBodyPreferenceChanged;
	}

	private void OnDisable()
	{
		_instances.Remove(this);
		if (PlayerCharacterMasterController.onPlayerRemoved != null)
		{
			PlayerCharacterMasterController.onPlayerRemoved(this);
		}
		NetworkUser.onNetworkUserBodyPreferenceChanged -= OnNetworkUserBodyPreferenceChanged;
	}

	private void Start()
	{
		if (NetworkServer.active && (bool)networkUser)
		{
			CallRpcIncrementRunCount();
		}
	}

	[ClientRpc]
	private void RpcIncrementRunCount()
	{
		if ((bool)networkUser)
		{
			LocalUser localUser = networkUser.localUser;
			if (localUser != null)
			{
				localUser.userProfile.totalRunCount++;
			}
		}
	}

	private static bool CanSendBodyInput(NetworkUser networkUser, out LocalUser localUser, out Player inputPlayer, out CameraRigController cameraRigController, out bool onlyAllowMovement)
	{
		if (!networkUser)
		{
			localUser = null;
			inputPlayer = null;
			cameraRigController = null;
			onlyAllowMovement = false;
			return false;
		}
		localUser = networkUser.localUser;
		inputPlayer = networkUser.inputPlayer;
		cameraRigController = networkUser.cameraRigController;
		onlyAllowMovement = false;
		if (localUser == null || inputPlayer == null || !cameraRigController)
		{
			return false;
		}
		if (localUser.isUIFocused)
		{
			UIInputPassthrough uIPassthrough = localUser.GetUIPassthrough();
			if (!uIPassthrough)
			{
				return false;
			}
			onlyAllowMovement = uIPassthrough.OnlyAllowMovement;
		}
		if (!cameraRigController.isControlAllowed)
		{
			return false;
		}
		return true;
	}

	private void Update()
	{
		if (!hasEffectiveAuthority)
		{
			return;
		}
		SetBody(master.GetBodyObject());
		if (!bodyInputs)
		{
			return;
		}
		Vector3 moveVector = Vector3.zero;
		Vector3 aimDirection = bodyInputs.aimDirection;
		if (CanSendBodyInput(networkUser, out var _, out var inputPlayer, out var cameraRigController, out var _))
		{
			Transform transform = cameraRigController.transform;
			sprintInputPressReceived |= inputPlayer.GetButtonDown(18);
			Vector2 vector = new Vector2(inputPlayer.GetAxis(0), inputPlayer.GetAxis(1));
			Vector2 vector2 = new Vector2(inputPlayer.GetAxis(12), inputPlayer.GetAxis(13));
			bodyInputs.SetRawMoveStates(vector + vector2);
			float sqrMagnitude = vector.sqrMagnitude;
			if (sqrMagnitude > 1f)
			{
				vector /= Mathf.Sqrt(sqrMagnitude);
			}
			if (bodyIsFlier)
			{
				moveVector = transform.right * vector.x + transform.forward * vector.y;
			}
			else
			{
				float y = transform.eulerAngles.y;
				moveVector = Quaternion.Euler(0f, y, 0f) * new Vector3(vector.x, 0f, vector.y);
			}
			aimDirection = (cameraRigController.crosshairWorldPosition - bodyInputs.aimOrigin).normalized;
		}
		bodyInputs.moveVector = moveVector;
		bodyInputs.aimDirection = aimDirection;
	}

	private void FixedUpdate()
	{
		if (hasEffectiveAuthority && (bool)bodyInputs)
		{
			PollButtonInput();
			CheckPinging();
		}
	}

	private void PollButtonInput()
	{
		bool newState = false;
		bool newState2 = false;
		bool newState3 = false;
		bool newState4 = false;
		bool newState5 = false;
		bool newState6 = false;
		bool newState7 = false;
		bool newState8 = false;
		bool newState9 = false;
		if (CanSendBodyInput(networkUser, out var _, out var inputPlayer, out var _, out var _))
		{
			bool flag = false;
			flag = body.isSprinting;
			if (sprintInputPressReceived)
			{
				sprintInputPressReceived = false;
				flag = !flag;
			}
			if (flag)
			{
				Vector3 aimDirection = bodyInputs.aimDirection;
				aimDirection.y = 0f;
				aimDirection.Normalize();
				Vector3 moveVector = bodyInputs.moveVector;
				moveVector.y = 0f;
				moveVector.Normalize();
				if ((body.bodyFlags & CharacterBody.BodyFlags.SprintAnyDirection) == 0 && Vector3.Dot(aimDirection, moveVector) < sprintMinAimMoveDot)
				{
					flag = false;
				}
			}
			newState = inputPlayer.GetButton(7);
			newState2 = inputPlayer.GetButton(8);
			newState3 = inputPlayer.GetButton(9);
			newState4 = inputPlayer.GetButton(10);
			newState5 = inputPlayer.GetButton(5);
			newState6 = inputPlayer.GetButton(4);
			newState7 = flag;
			newState8 = inputPlayer.GetButton(6);
			newState9 = inputPlayer.GetButton(28);
		}
		bodyInputs.skill1.PushState(newState);
		bodyInputs.skill2.PushState(newState2);
		bodyInputs.skill3.PushState(newState3);
		bodyInputs.skill4.PushState(newState4);
		bodyInputs.interact.PushState(newState5);
		bodyInputs.jump.PushState(newState6);
		bodyInputs.sprint.PushState(newState7);
		bodyInputs.activateEquipment.PushState(newState8);
		bodyInputs.ping.PushState(newState9);
	}

	public void RedirectCamera(Vector3 desiredRotation)
	{
		if (CanSendBodyInput(networkUser, out var _, out var _, out var cameraRigController, out var _))
		{
			CameraState cameraStateToMatch = cameraRigController.currentCameraState;
			cameraStateToMatch.rotation = Quaternion.Euler(desiredRotation.x, desiredRotation.y, desiredRotation.z);
			CameraModeBase.CameraModeContext context = default(CameraModeBase.CameraModeContext);
			context.cameraInfo.cameraRigController = cameraRigController;
			cameraRigController.cameraMode?.MatchState(in context, in cameraStateToMatch);
		}
	}

	private void CheckPinging()
	{
		if (hasEffectiveAuthority && (bool)body && (bool)bodyInputs && bodyInputs.ping.justPressed)
		{
			pingerController.AttemptPing(new Ray(bodyInputs.aimOrigin, bodyInputs.aimDirection), body.gameObject);
		}
	}

	public string GetDisplayName()
	{
		string result = "";
		if ((bool)networkUserObject)
		{
			NetworkUser component = networkUserObject.GetComponent<NetworkUser>();
			if ((bool)component)
			{
				result = component.userName;
			}
		}
		return result;
	}

	private void SetBody(GameObject newBody)
	{
		if ((bool)newBody)
		{
			body = newBody.GetComponent<CharacterBody>();
		}
		else
		{
			body = null;
		}
		if ((bool)body)
		{
			bodyInputs = body.inputBank;
			bodyMotor = body.characterMotor;
		}
		else
		{
			bodyInputs = null;
			bodyMotor = null;
		}
	}

	[Server]
	public void OnBodyDeath()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PlayerCharacterMasterController::OnBodyDeath()' called on client");
		}
	}

	public void OnBodyStart()
	{
		if (NetworkServer.active)
		{
			finalMessageTokenServer = string.Empty;
		}
	}

	public static int GetPlayersWithBodiesCount()
	{
		int num = 0;
		foreach (PlayerCharacterMasterController instance in _instances)
		{
			if (instance.master.hasBody)
			{
				num++;
			}
		}
		return num;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		GlobalEventManager.onCharacterDeathGlobal += delegate(DamageReport damageReport)
		{
			CharacterMaster characterMaster = damageReport.attackerMaster;
			if ((bool)characterMaster)
			{
				if ((bool)characterMaster.minionOwnership.ownerMaster)
				{
					characterMaster = characterMaster.minionOwnership.ownerMaster;
				}
				PlayerCharacterMasterController component = characterMaster.GetComponent<PlayerCharacterMasterController>();
				if ((bool)component && Util.CheckRoll(1f * component.lunarCoinChanceMultiplier))
				{
					PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.MiscPickups.LunarCoin.miscPickupIndex), damageReport.victim.transform.position, Vector3.up * 10f);
					component.lunarCoinChanceMultiplier *= 0.5f;
				}
			}
		};
	}

	private void SetBodyPrefabToPreference()
	{
		master.bodyPrefab = BodyCatalog.GetBodyPrefab(networkUser.bodyIndexPreference);
		if (!master.bodyPrefab)
		{
			Debug.LogError($"SetBodyPrefabToPreference failed to find a body prefab for index '{networkUser.bodyIndexPreference}'.  Reverting to backup: {master.backupBodyIndex}");
			master.bodyPrefab = BodyCatalog.GetBodyPrefab(master.backupBodyIndex);
			if (!master.bodyPrefab)
			{
				Debug.LogError($"SetBodyPrefabToPreference backup ({master.backupBodyIndex}) failed.");
			}
		}
	}

	private void OnNetworkUserBodyPreferenceChanged(NetworkUser networkUser)
	{
		if ((bool)this.networkUser && networkUser.netId.Value == this.networkUser.netId.Value)
		{
			SetBodyPrefabToPreference();
		}
	}

	static PlayerCharacterMasterController()
	{
		_instances = new List<PlayerCharacterMasterController>();
		_instancesReadOnly = new ReadOnlyCollection<PlayerCharacterMasterController>(_instances);
		sprintMinAimMoveDot = Mathf.Cos(MathF.PI / 3f);
		kRpcRpcOnLinkedToNetworkUser = 585837834;
		NetworkBehaviour.RegisterRpcDelegate(typeof(PlayerCharacterMasterController), kRpcRpcOnLinkedToNetworkUser, InvokeRpcRpcOnLinkedToNetworkUser);
		kRpcRpcIncrementRunCount = 1915650359;
		NetworkBehaviour.RegisterRpcDelegate(typeof(PlayerCharacterMasterController), kRpcRpcIncrementRunCount, InvokeRpcRpcIncrementRunCount);
		NetworkCRC.RegisterBehaviour("PlayerCharacterMasterController", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcOnLinkedToNetworkUser(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnLinkedToNetworkUser called on server.");
		}
		else
		{
			((PlayerCharacterMasterController)obj).RpcOnLinkedToNetworkUser(GeneratedNetworkCode._ReadNetworkUserId_None(reader));
		}
	}

	protected static void InvokeRpcRpcIncrementRunCount(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcIncrementRunCount called on server.");
		}
		else
		{
			((PlayerCharacterMasterController)obj).RpcIncrementRunCount();
		}
	}

	public void CallRpcOnLinkedToNetworkUser(NetworkUserId id)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnLinkedToNetworkUser called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnLinkedToNetworkUser);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteNetworkUserId_None(networkWriter, id);
		SendRPCInternal(networkWriter, 0, "RpcOnLinkedToNetworkUser");
	}

	public void CallRpcIncrementRunCount()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcIncrementRunCount called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcIncrementRunCount);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcIncrementRunCount");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(networkUserInstanceId);
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
			writer.Write(networkUserInstanceId);
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
			networkUserInstanceId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncNetworkUserInstanceId(reader.ReadNetworkId());
		}
	}

	public override void PreStartClient()
	{
	}
}
