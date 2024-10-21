using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EntityStates;
using EntityStates.LightningStorm;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

[RequireComponent(typeof(EntityStateMachine))]
public class LightningStormController : NetworkBehaviour
{
	[Serializable]
	public struct LightningStrikePoint
	{
		[Tooltip("Just for identifying points - not used in code")]
		public string description;

		[Tooltip("How far from the target's position should this point appear.")]
		public float distanceFromTargetPosition;

		[FormerlySerializedAs("distanceProjectionMultiplier")]
		[FormerlySerializedAs("speedMultiplier")]
		[Tooltip("How much the projected position should take into account the player's current velocity. 1.8 tends to work if you want it to go off right as they pass by it.")]
		public float velocityMultiplier;

		[Tooltip("Degrees to offset the prstrike position (0 = forward, 180 = behind, 90 = right, 270 / -90 = left, etc)")]
		public float directionOffset;

		[Tooltip("A randomized number of degrees (+/-) to adjust direction offset by.")]
		public float randomDegreeOffset;
	}

	public static LightningStrikePattern lightningPattern;

	[SyncVar(hook = "Client_HandleStormActiveChanged")]
	public bool stormActive;

	[SyncVar(hook = "Client_SetStormStrikePattern")]
	private int strikePatternID = -1;

	[SerializeField]
	private List<LightningStrikePattern> strikePatterns = new List<LightningStrikePattern>();

	[SerializeField]
	[Space(20f)]
	private EntityStateMachine stormStateMachine;

	[SerializeField]
	public GameObject startPostProcessingObject;

	[SerializeField]
	public PostProcessDuration startPostProcessDuration;

	[SerializeField]
	public GameObject endPostProcessingObject;

	[SerializeField]
	public PostProcessDuration endPostProcessDuration;

	[FormerlySerializedAs("serializedlightningPattern")]
	[SerializeField]
	public LightningStrikePattern fallbackLightningPattern;

	private static int kCmdCmdSetStormActive;

	private static int kCmdCmdSetStrikePattern;

	public static LightningStormController instance { get; private set; }

	public bool NetworkstormActive
	{
		get
		{
			return stormActive;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				Client_HandleStormActiveChanged(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref stormActive, 1u);
		}
	}

	public int NetworkstrikePatternID
	{
		get
		{
			return strikePatternID;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				Client_SetStormStrikePattern(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref strikePatternID, 2u);
		}
	}

	public static void SetStormActive(bool b)
	{
		if (!(instance == null) && b != instance.stormActive)
		{
			if (!NetworkServer.active)
			{
				instance.CallCmdSetStormActive(b);
			}
			else
			{
				instance.ServerSetStormActive(b);
			}
		}
	}

	public static void SetStormStrikePattern(LightningStrikePattern _pattern)
	{
		if (!(instance == null) && !(_pattern == null) && !(_pattern == lightningPattern) && _pattern != null)
		{
			if (NetworkServer.active)
			{
				instance.ServerSetStrikePattern(_pattern.ID);
			}
			else
			{
				instance.CallCmdSetStrikePattern(_pattern.ID);
			}
		}
	}

	public static LightningStrikeInstance FireLightningBolt(Vector3 position, GameObject lightningPrefab, BlastAttack _blastInfo = null)
	{
		if (instance != null && !instance.stormActive)
		{
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(lightningPrefab);
		LightningStrikeInstance component = gameObject.GetComponent<LightningStrikeInstance>();
		if (!component)
		{
			UnityEngine.Object.Destroy(gameObject);
			return null;
		}
		component.Initialize(position, _blastInfo);
		return component;
	}

	public static bool ShouldFireLighting()
	{
		if (instance == null)
		{
			return false;
		}
		return instance.stormActive;
	}

	public static Vector3 PredictPosition(Vector3 position, Vector3 forwardDirection, float maxSpeed, LightningStrikePoint strikePointData)
	{
		forwardDirection = Quaternion.AngleAxis(strikePointData.directionOffset, Vector3.up) * forwardDirection;
		if (strikePointData.randomDegreeOffset != 0f)
		{
			forwardDirection = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - strikePointData.randomDegreeOffset, strikePointData.randomDegreeOffset), Vector3.up) * forwardDirection;
		}
		position += forwardDirection * maxSpeed * strikePointData.velocityMultiplier;
		position += forwardDirection * strikePointData.distanceFromTargetPosition;
		return position;
	}

	public static float GetMaxDistance(Vector3 forwardDirection, float maxSpeed, LightningStrikePoint strikePointData)
	{
		return (forwardDirection * maxSpeed * strikePointData.velocityMultiplier).magnitude + strikePointData.distanceFromTargetPosition;
	}

	[ContextMenu("Load Current Lightning Pattern Scrob into Memory")]
	public void LoadCurrentLightningPatternScrob()
	{
		SetStormStrikePattern(fallbackLightningPattern);
	}

	[Command]
	private void CmdSetStormActive(bool b)
	{
		ServerSetStormActive(b);
	}

	[Server]
	private void ServerSetStormActive(bool b)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.LightningStormController::ServerSetStormActive(System.Boolean)' called on client");
			return;
		}
		if (b != stormActive)
		{
			if (b)
			{
				HandleStormBegins();
			}
			else
			{
				HandleStormEnds();
			}
		}
		NetworkstormActive = b;
	}

	private void Client_HandleStormActiveChanged(bool newValue)
	{
		if (NetworkServer.active)
		{
			return;
		}
		if (newValue != stormActive)
		{
			if (newValue)
			{
				HandleStormBegins();
			}
			else
			{
				HandleStormEnds();
			}
		}
		NetworkstormActive = newValue;
	}

	[Server]
	private void ServerSetStrikePattern(int patternID)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.LightningStormController::ServerSetStrikePattern(System.Int32)' called on client");
			return;
		}
		if (patternID >= strikePatterns.Count || patternID < 0)
		{
			patternID = 0;
		}
		if (patternID != strikePatternID)
		{
			NetworkstrikePatternID = patternID;
			lightningPattern = strikePatterns[patternID];
		}
	}

	[Command]
	private void CmdSetStrikePattern(int patternID)
	{
		ServerSetStrikePattern(patternID);
	}

	private void Client_SetStormStrikePattern(int newPatternIndex)
	{
		if (NetworkServer.active)
		{
			return;
		}
		if (newPatternIndex >= strikePatterns.Count || newPatternIndex < 0)
		{
			newPatternIndex = 0;
			if (strikePatterns.Count < 1)
			{
				return;
			}
		}
		lightningPattern = strikePatterns[newPatternIndex];
		NetworkstrikePatternID = newPatternIndex;
	}

	private void Awake()
	{
		if (strikePatterns.Count >= 1)
		{
			for (int i = 0; i < strikePatterns.Count; i++)
			{
				strikePatterns[i].ID = i;
			}
			int num = ((fallbackLightningPattern != null) ? fallbackLightningPattern.ID : 0);
			if (NetworkServer.active)
			{
				ServerSetStrikePattern(num);
			}
			else
			{
				lightningPattern = strikePatterns[num];
			}
		}
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		if (stormActive)
		{
			HandleStormBegins();
		}
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
		HandleStormEnds();
	}

	private void HandleStormBegins()
	{
		if (!(lightningPattern == null))
		{
			SetStormStrikePattern(fallbackLightningPattern);
			stormStateMachine.SetNextState(new LightningStormState());
		}
	}

	private void HandleStormEnds()
	{
		stormStateMachine.SetNextState(new Idle());
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdSetStormActive(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetStormActive called on client.");
		}
		else
		{
			((LightningStormController)obj).CmdSetStormActive(reader.ReadBoolean());
		}
	}

	protected static void InvokeCmdCmdSetStrikePattern(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetStrikePattern called on client.");
		}
		else
		{
			((LightningStormController)obj).CmdSetStrikePattern((int)reader.ReadPackedUInt32());
		}
	}

	public void CallCmdSetStormActive(bool b)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetStormActive called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetStormActive(b);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetStormActive);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(b);
		SendCommandInternal(networkWriter, 0, "CmdSetStormActive");
	}

	public void CallCmdSetStrikePattern(int patternID)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetStrikePattern called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetStrikePattern(patternID);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetStrikePattern);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)patternID);
		SendCommandInternal(networkWriter, 0, "CmdSetStrikePattern");
	}

	static LightningStormController()
	{
		kCmdCmdSetStormActive = 753869832;
		NetworkBehaviour.RegisterCommandDelegate(typeof(LightningStormController), kCmdCmdSetStormActive, InvokeCmdCmdSetStormActive);
		kCmdCmdSetStrikePattern = 1007190007;
		NetworkBehaviour.RegisterCommandDelegate(typeof(LightningStormController), kCmdCmdSetStrikePattern, InvokeCmdCmdSetStrikePattern);
		NetworkCRC.RegisterBehaviour("LightningStormController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(stormActive);
			writer.WritePackedUInt32((uint)strikePatternID);
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
			writer.Write(stormActive);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)strikePatternID);
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
			stormActive = reader.ReadBoolean();
			strikePatternID = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			Client_HandleStormActiveChanged(reader.ReadBoolean());
		}
		if (((uint)num & 2u) != 0)
		{
			Client_SetStormStrikePattern((int)reader.ReadPackedUInt32());
		}
	}

	public override void PreStartClient()
	{
	}
}
