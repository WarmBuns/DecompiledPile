using System;
using System.Collections.Generic;
using RoR2.HudOverlay;
using RoR2.Navigation;
using RoR2.Projectile;
using RoR2.UI;
using Unity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class SeekerController : NetworkBehaviour, IOnKilledServerReceiver
{
	private byte currentChakraGate;

	public bool doesExitVehicle;

	[SerializeField]
	public GameObject healingExplosionPrefab;

	[SerializeField]
	public GameObject scopeOverlayPrefab;

	[SerializeField]
	public string overlayChild;

	private OverlayController overlayController;

	private List<ImageFillController> fillUiList = new List<ImageFillController>();

	private ChildLocator overlayInstanceChildLocator;

	public Color32 chakraDefaultColor = new Color32(0, 0, 0, 128);

	public Color32 chakraGrantedColor = new Color32(byte.MaxValue, 233, 124, 200);

	public Color32 chakraFinishedColor = new Color32(0, byte.MaxValue, 0, 200);

	public Color32 chakraAfterReviveColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125);

	public Color32 chakraAfterReviveCompleteColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 200);

	public Color32 chakraSplashDefaultColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125);

	public Color32 chakraSplashGrantedColor = new Color32(116, 226, 203, 200);

	public Color32 chakraSplashBurnedColor = new Color32(0, 0, 0, 0);

	public Color32 chakraBurnedColor = new Color32(byte.MaxValue, 0, 0, 200);

	public string[] chakraOrder = new string[7] { "Pedal1", "Pedal7", "Pedal2", "Pedal6", "Pedal3", "Pedal5", "Pedal4" };

	public string finalActivation = "Glow";

	[Tooltip("If a new respawn point is needed from a dead player, how far from the Seeker can it be.")]
	public float newRespawnPointMaxDistanceFromSeeker = 10f;

	[Tooltip("In case we need a new respawn point, we may only get one. So we randomize positions around that point for safety. Not too high: they might spawn through a wall.")]
	public Vector3 randomDistanceForPlayerRespawn = new Vector3(2f, 1f, 2f);

	private SeekerSoulSpiralManager soulSpiralManager;

	private CharacterBody characterBody;

	[NonSerialized]
	public sbyte[] meditationStepAndSequence = new sbyte[6];

	[NonSerialized]
	public bool meditationUIDirty;

	private List<NodeGraph.NodeIndex> possibleNodesToTarget = new List<NodeGraph.NodeIndex>();

	private static int kCmdCmdUnlockGateEffects;

	private static int kCmdCmdUpdateMeditationInput;

	private static int kRpcRpcSetMeditation;

	private static int kRpcRpcSetPedalsValue;

	private static int kRpcRpcSpiralCommand;

	private static int kCmdCmdTriggerHealPulse;

	private static int kCmdCmdSpiralCommand;

	public sbyte meditationInputStep
	{
		get
		{
			return meditationStepAndSequence[0];
		}
		set
		{
			meditationUIDirty = true;
			meditationStepAndSequence[0] = value;
			CallCmdUpdateMeditationInput(meditationStepAndSequence);
		}
	}

	public sbyte GetNextMeditationStep()
	{
		int num = meditationStepAndSequence[0] + 1;
		if (0 <= num && num >= meditationStepAndSequence.Length)
		{
			return -1;
		}
		return meditationStepAndSequence[num];
	}

	private void OnEnable()
	{
		overlayController = HudOverlayManager.AddOverlay(base.gameObject, new OverlayCreationParams
		{
			prefab = scopeOverlayPrefab,
			childLocatorEntry = overlayChild
		});
		overlayController.onInstanceAdded += OnOverlayInstanceAdded;
		overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
		if (soulSpiralManager == null)
		{
			soulSpiralManager = new SeekerSoulSpiralManager(this);
		}
		characterBody = GetComponent<CharacterBody>();
	}

	private bool IsPositionWithinBounds(Vector3 position)
	{
		List<MapZone> instancesList = InstanceTracker.GetInstancesList<MapZone>();
		bool flag = false;
		int num = 0;
		foreach (MapZone item in instancesList)
		{
			if (item.gameObject.activeSelf && item.zoneType == MapZone.ZoneType.OutOfBounds && item.triggerType == MapZone.TriggerType.TriggerExit)
			{
				num++;
				flag |= item.IsPointInsideMapZone(position);
			}
		}
		if (num < 1)
		{
			return true;
		}
		return flag;
	}

	private Vector3 FindNewSpawnPosition(Vector3 spawnPosition)
	{
		Vector3 footPosition = characterBody.footPosition;
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		if (possibleNodesToTarget.Count < 1)
		{
			newRespawnPointMaxDistanceFromSeeker = Mathf.Max(newRespawnPointMaxDistanceFromSeeker, 1f);
			groundNodes.FindNodesInRange(footPosition, 1f, newRespawnPointMaxDistanceFromSeeker, HullMask.Human, possibleNodesToTarget);
		}
		if (possibleNodesToTarget.Count < 1)
		{
			return spawnPosition;
		}
		groundNodes.GetNodePosition(possibleNodesToTarget[^1], out var position);
		possibleNodesToTarget.RemoveAt(possibleNodesToTarget.Count - 1);
		return position;
	}

	[Command]
	private void CmdUnlockGateEffects(byte chakraGate)
	{
		currentChakraGate = chakraGate;
		CharacterBody characterBody = this.characterBody;
		if (chakraGate < 7)
		{
			characterBody.AddBuff(DLC2Content.Buffs.ChakraBuff.buffIndex);
		}
		else if (chakraGate == 7 && characterBody.master.getSeekerUsedRevive() == CharacterMaster.SEEKER_REVIVE_STATUS.UNUSED)
		{
			foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
			{
				if (instance == null)
				{
					continue;
				}
				CharacterMaster master = instance.master;
				if (instance.isConnected && master.IsDeadAndOutOfLivesServer())
				{
					Vector3 vector = master.deathFootPosition;
					if (!IsPositionWithinBounds(vector))
					{
						vector = FindNewSpawnPosition(vector);
						vector.y += UnityEngine.Random.Range(0.5f, Mathf.Abs(randomDistanceForPlayerRespawn.y));
						vector.x += UnityEngine.Random.Range(0f - randomDistanceForPlayerRespawn.x, randomDistanceForPlayerRespawn.x);
						vector.z += UnityEngine.Random.Range(0f - randomDistanceForPlayerRespawn.z, randomDistanceForPlayerRespawn.z);
					}
					master.Respawn(vector, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), wasRevivedMidStage: true);
					CharacterBody body = master.GetBody();
					if ((bool)body)
					{
						body.AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
						EntityStateMachine[] components = body.GetComponents<EntityStateMachine>();
						foreach (EntityStateMachine obj in components)
						{
							obj.initialStateType = obj.mainStateType;
						}
						GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/TeleporterHealNovaPulse");
						if ((bool)gameObject)
						{
							EffectManager.SpawnEffect(gameObject, new EffectData
							{
								origin = vector,
								rotation = body.transform.rotation
							}, transmit: true);
						}
					}
				}
				else if ((object)master == null || master.GetBody()?.HasBuff(DLC2Content.Buffs.RevitalizeBuff.buffIndex) != true)
				{
					master?.GetBody()?.AddBuff(DLC2Content.Buffs.RevitalizeBuff.buffIndex);
				}
			}
			GetComponent<CharacterBody>().master.CallRpcSetSeekerRevive(CharacterMaster.SEEKER_REVIVE_STATUS.USED);
		}
		CallRpcSetPedalsValue(chakraGate);
	}

	public void RandomizeMeditationInputs()
	{
		meditationStepAndSequence[0] = 0;
		for (int i = 1; i <= 5; i++)
		{
			meditationStepAndSequence[i] = (sbyte)UnityEngine.Random.Range(0, 4);
		}
		CallCmdUpdateMeditationInput(meditationStepAndSequence);
	}

	[Command]
	private void CmdUpdateMeditationInput(sbyte[] sequence)
	{
		meditationStepAndSequence = sequence;
		meditationUIDirty = true;
		CallRpcSetMeditation(sequence);
	}

	[ClientRpc]
	private void RpcSetMeditation(sbyte[] sequence)
	{
		meditationStepAndSequence = sequence;
		meditationUIDirty = true;
	}

	public void IncrementChakraGate()
	{
		MonoBehaviour.print("triggered unlock gate");
		currentChakraGate++;
		CallCmdUnlockGateEffects(currentChakraGate);
	}

	[ClientRpc]
	public void RpcSetPedalsValue(byte value)
	{
		if (base.hasAuthority)
		{
			currentChakraGate = value;
			UpdatePetalsUI();
		}
	}

	public void UpdatePetalsUI()
	{
		CharacterMaster.SEEKER_REVIVE_STATUS seekerUsedRevive = GetComponent<CharacterBody>().master.getSeekerUsedRevive();
		Image component = overlayInstanceChildLocator.FindChild(finalActivation).GetComponent<Image>();
		switch (seekerUsedRevive)
		{
		case CharacterMaster.SEEKER_REVIVE_STATUS.UNUSED:
			component.color = chakraSplashDefaultColor;
			setColors(chakraGrantedColor, chakraDefaultColor);
			break;
		case CharacterMaster.SEEKER_REVIVE_STATUS.USED:
			component.color = chakraSplashGrantedColor;
			setColors(chakraFinishedColor, chakraDefaultColor);
			break;
		case CharacterMaster.SEEKER_REVIVE_STATUS.BURNED:
			component.color = chakraSplashBurnedColor;
			if (currentChakraGate >= 7)
			{
				setColors(chakraAfterReviveCompleteColor, chakraDefaultColor);
				break;
			}
			currentChakraGate = (byte)((currentChakraGate < 1) ? 1 : currentChakraGate);
			setColors(chakraAfterReviveColor, chakraDefaultColor);
			break;
		}
		void setColors(Color32 active, Color32 inactive)
		{
			for (int i = 0; i < 7; i++)
			{
				if (i < currentChakraGate)
				{
					overlayInstanceChildLocator.FindChild(chakraOrder[i]).GetComponent<Image>().color = active;
				}
				else
				{
					overlayInstanceChildLocator.FindChild(chakraOrder[i]).GetComponent<Image>().color = inactive;
				}
			}
		}
	}

	private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
	{
		fillUiList.Add(instance.GetComponent<ImageFillController>());
		overlayInstanceChildLocator = instance.GetComponent<ChildLocator>();
		UpdatePetalsUI();
	}

	private void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
	{
		fillUiList.Remove(instance.GetComponent<ImageFillController>());
	}

	public void FireSoulSpiral(FireProjectileInfo projectileInfo)
	{
		soulSpiralManager.FireSoulSpiral(projectileInfo);
	}

	public void SpiralSendCommand(SeekerSoulSpiralManager.SoulSpiralCommandType msg)
	{
		if (NetworkServer.active)
		{
			CallRpcSpiralCommand(msg);
		}
		else
		{
			CallCmdSpiralCommand(msg);
		}
	}

	[ClientRpc]
	public void RpcSpiralCommand(SeekerSoulSpiralManager.SoulSpiralCommandType msg)
	{
		if (!soulSpiralManager.hasAuthority && !NetworkServer.active)
		{
			soulSpiralManager.ReceiveSpiralCommand(msg);
		}
	}

	[Command]
	internal void CmdTriggerHealPulse(float value, Vector3 corePosition, float blastRadius)
	{
		HealingPulse healingPulse = new HealingPulse();
		healingPulse.healAmount = value;
		healingPulse.origin = corePosition;
		healingPulse.radius = blastRadius;
		healingPulse.effectPrefab = healingExplosionPrefab;
		healingPulse.teamIndex = TeamIndex.Player;
		healingPulse.overShield = 0f;
		healingPulse.Fire();
	}

	[Command]
	public void CmdSpiralCommand(SeekerSoulSpiralManager.SoulSpiralCommandType msg)
	{
		if (!soulSpiralManager.hasAuthority)
		{
			soulSpiralManager.ReceiveSpiralCommand(msg);
		}
		CallRpcSpiralCommand(msg);
	}

	private void FixedUpdate()
	{
		if (soulSpiralManager.runFixedUpdate)
		{
			soulSpiralManager.MyFixedUpdate();
		}
	}

	private void OnDestroy()
	{
		soulSpiralManager.ShutdownAndCleanupSpirals();
	}

	public void OnKilledServer(DamageReport damageReport)
	{
		if (NetworkServer.active)
		{
			soulSpiralManager.ShutdownAndCleanupSpirals();
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdUnlockGateEffects(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUnlockGateEffects called on client.");
		}
		else
		{
			((SeekerController)obj).CmdUnlockGateEffects((byte)reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeCmdCmdUpdateMeditationInput(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpdateMeditationInput called on client.");
		}
		else
		{
			((SeekerController)obj).CmdUpdateMeditationInput(GeneratedNetworkCode._ReadArraySByte_None(reader));
		}
	}

	protected static void InvokeCmdCmdTriggerHealPulse(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdTriggerHealPulse called on client.");
		}
		else
		{
			((SeekerController)obj).CmdTriggerHealPulse(reader.ReadSingle(), reader.ReadVector3(), reader.ReadSingle());
		}
	}

	protected static void InvokeCmdCmdSpiralCommand(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSpiralCommand called on client.");
		}
		else
		{
			((SeekerController)obj).CmdSpiralCommand((SeekerSoulSpiralManager.SoulSpiralCommandType)reader.ReadInt32());
		}
	}

	public void CallCmdUnlockGateEffects(byte chakraGate)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdUnlockGateEffects called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdUnlockGateEffects(chakraGate);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdUnlockGateEffects);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32(chakraGate);
		SendCommandInternal(networkWriter, 0, "CmdUnlockGateEffects");
	}

	public void CallCmdUpdateMeditationInput(sbyte[] sequence)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdUpdateMeditationInput called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdUpdateMeditationInput(sequence);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdUpdateMeditationInput);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteArraySByte_None(networkWriter, sequence);
		SendCommandInternal(networkWriter, 0, "CmdUpdateMeditationInput");
	}

	public void CallCmdTriggerHealPulse(float value, Vector3 corePosition, float blastRadius)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdTriggerHealPulse called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdTriggerHealPulse(value, corePosition, blastRadius);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdTriggerHealPulse);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(value);
		networkWriter.Write(corePosition);
		networkWriter.Write(blastRadius);
		SendCommandInternal(networkWriter, 0, "CmdTriggerHealPulse");
	}

	public void CallCmdSpiralCommand(SeekerSoulSpiralManager.SoulSpiralCommandType msg)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSpiralCommand called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSpiralCommand(msg);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSpiralCommand);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)msg);
		SendCommandInternal(networkWriter, 0, "CmdSpiralCommand");
	}

	protected static void InvokeRpcRpcSetMeditation(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetMeditation called on server.");
		}
		else
		{
			((SeekerController)obj).RpcSetMeditation(GeneratedNetworkCode._ReadArraySByte_None(reader));
		}
	}

	protected static void InvokeRpcRpcSetPedalsValue(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetPedalsValue called on server.");
		}
		else
		{
			((SeekerController)obj).RpcSetPedalsValue((byte)reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeRpcRpcSpiralCommand(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSpiralCommand called on server.");
		}
		else
		{
			((SeekerController)obj).RpcSpiralCommand((SeekerSoulSpiralManager.SoulSpiralCommandType)reader.ReadInt32());
		}
	}

	public void CallRpcSetMeditation(sbyte[] sequence)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetMeditation called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetMeditation);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteArraySByte_None(networkWriter, sequence);
		SendRPCInternal(networkWriter, 0, "RpcSetMeditation");
	}

	public void CallRpcSetPedalsValue(byte value)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetPedalsValue called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetPedalsValue);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32(value);
		SendRPCInternal(networkWriter, 0, "RpcSetPedalsValue");
	}

	public void CallRpcSpiralCommand(SeekerSoulSpiralManager.SoulSpiralCommandType msg)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSpiralCommand called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSpiralCommand);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)msg);
		SendRPCInternal(networkWriter, 0, "RpcSpiralCommand");
	}

	static SeekerController()
	{
		kCmdCmdUnlockGateEffects = -1757393198;
		NetworkBehaviour.RegisterCommandDelegate(typeof(SeekerController), kCmdCmdUnlockGateEffects, InvokeCmdCmdUnlockGateEffects);
		kCmdCmdUpdateMeditationInput = -1791025022;
		NetworkBehaviour.RegisterCommandDelegate(typeof(SeekerController), kCmdCmdUpdateMeditationInput, InvokeCmdCmdUpdateMeditationInput);
		kCmdCmdTriggerHealPulse = -416543526;
		NetworkBehaviour.RegisterCommandDelegate(typeof(SeekerController), kCmdCmdTriggerHealPulse, InvokeCmdCmdTriggerHealPulse);
		kCmdCmdSpiralCommand = -1428889191;
		NetworkBehaviour.RegisterCommandDelegate(typeof(SeekerController), kCmdCmdSpiralCommand, InvokeCmdCmdSpiralCommand);
		kRpcRpcSetMeditation = -72748599;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SeekerController), kRpcRpcSetMeditation, InvokeRpcRpcSetMeditation);
		kRpcRpcSetPedalsValue = 1863576493;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SeekerController), kRpcRpcSetPedalsValue, InvokeRpcRpcSetPedalsValue);
		kRpcRpcSpiralCommand = 1157507459;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SeekerController), kRpcRpcSpiralCommand, InvokeRpcRpcSpiralCommand);
		NetworkCRC.RegisterBehaviour("SeekerController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
