using System.Collections.Generic;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Stats;

[RequireComponent(typeof(CharacterMaster))]
[RequireComponent(typeof(PlayerCharacterMasterController))]
public class PlayerStatsComponent : NetworkBehaviour
{
	public static readonly List<PlayerStatsComponent> instancesList = new List<PlayerStatsComponent>();

	private float serverTransmitTimer;

	private float serverTransmitInterval = 10f;

	private Vector3 previousBodyPosition;

	private GameObject cachedBodyObject;

	private CharacterBody cachedCharacterBody;

	private CharacterMotor cachedBodyCharacterMotor;

	private Transform cachedBodyTransform;

	public StatSheet currentStats;

	private StatSheet clientDeltaStatsBuffer;

	private StatSheet recordedStats;

	public CharacterMaster characterMaster { get; private set; }

	public PlayerCharacterMasterController playerCharacterMasterController { get; private set; }

	private void Awake()
	{
		playerCharacterMasterController = GetComponent<PlayerCharacterMasterController>();
		characterMaster = GetComponent<CharacterMaster>();
		instancesList.Add(this);
		currentStats = StatSheet.New();
		if (NetworkClient.active)
		{
			recordedStats = StatSheet.New();
			clientDeltaStatsBuffer = StatSheet.New();
		}
	}

	private void OnDestroy()
	{
		if (NetworkServer.active)
		{
			SendUpdateToClient();
		}
		instancesList.Remove(this);
	}

	public static StatSheet FindBodyStatSheet(GameObject bodyObject)
	{
		if (!bodyObject)
		{
			return null;
		}
		return FindBodyStatSheet(bodyObject.GetComponent<CharacterBody>());
	}

	public static StatSheet FindBodyStatSheet(CharacterBody characterBody)
	{
		return characterBody?.master?.GetComponent<PlayerStatsComponent>()?.currentStats;
	}

	public static StatSheet FindMasterStatSheet(CharacterMaster master)
	{
		return FindMasterStatsComponent(master)?.currentStats;
	}

	public static PlayerStatsComponent FindBodyStatsComponent(GameObject bodyObject)
	{
		if (!bodyObject)
		{
			return null;
		}
		return FindBodyStatsComponent(bodyObject.GetComponent<CharacterBody>());
	}

	public static PlayerStatsComponent FindBodyStatsComponent(CharacterBody characterBody)
	{
		return characterBody?.master?.GetComponent<PlayerStatsComponent>();
	}

	public static PlayerStatsComponent FindMasterStatsComponent(CharacterMaster master)
	{
		return master?.playerStatsComponent;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		GlobalEventManager.onCharacterDeathGlobal += delegate(DamageReport damageReport)
		{
			if (NetworkServer.active)
			{
				PlayerStatsComponent playerStatsComponent = FindBodyStatsComponent(damageReport.victim.gameObject);
				if ((bool)playerStatsComponent)
				{
					playerStatsComponent.serverTransmitTimer = 0f;
				}
			}
		};
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			ServerFixedUpdate();
		}
	}

	[Server]
	public void ForceNextTransmit()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Stats.PlayerStatsComponent::ForceNextTransmit()' called on client");
		}
		else
		{
			serverTransmitTimer = 0f;
		}
	}

	[Server]
	private void ServerFixedUpdate()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Stats.PlayerStatsComponent::ServerFixedUpdate()' called on client");
			return;
		}
		float num = 0f;
		float runTime = 0f;
		if ((bool)Run.instance && !Run.instance.isRunStopwatchPaused)
		{
			num = Time.fixedDeltaTime;
			runTime = Run.instance.GetRunStopwatch();
		}
		StatManager.CharacterUpdateEvent e = default(StatManager.CharacterUpdateEvent);
		e.statsComponent = this;
		e.runTime = runTime;
		GameObject bodyObject = characterMaster.GetBodyObject();
		if (bodyObject != cachedBodyObject)
		{
			cachedBodyObject = bodyObject;
			cachedBodyObject = bodyObject;
			cachedBodyTransform = bodyObject?.transform;
			if ((bool)cachedBodyTransform)
			{
				previousBodyPosition = cachedBodyTransform.position;
			}
			cachedCharacterBody = bodyObject?.GetComponent<CharacterBody>();
			cachedBodyCharacterMotor = bodyObject?.GetComponent<CharacterMotor>();
		}
		if ((bool)cachedBodyTransform)
		{
			Vector3 position = cachedBodyTransform.position;
			e.additionalDistanceTraveled = Vector3.Distance(position, previousBodyPosition);
			previousBodyPosition = position;
		}
		if (characterMaster.hasBody)
		{
			e.additionalTimeAlive += num;
		}
		if ((bool)cachedCharacterBody)
		{
			e.level = (int)cachedCharacterBody.level;
		}
		StatManager.PushCharacterUpdateEvent(e);
		serverTransmitTimer -= Time.fixedDeltaTime;
		if (serverTransmitTimer <= 0f)
		{
			serverTransmitTimer = serverTransmitInterval;
			SendUpdateToClient();
		}
	}

	[Server]
	private void SendUpdateToClient()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Stats.PlayerStatsComponent::SendUpdateToClient()' called on client");
			return;
		}
		NetworkUser networkUser = playerCharacterMasterController.networkUser;
		if ((bool)networkUser)
		{
			NetworkWriter networkWriter = new NetworkWriter();
			networkWriter.StartMessage(58);
			networkWriter.Write(base.gameObject);
			currentStats.WriteChanges(networkWriter);
			networkWriter.FinishMessage();
			networkUser.connectionToClient.SendWriter(networkWriter, GetNetworkChannel());
		}
	}

	[NetworkMessageHandler(client = true, msgType = 58)]
	private static void HandleStatsUpdate(NetworkMessage netMsg)
	{
		GameObject gameObject = netMsg.reader.ReadGameObject();
		if ((bool)gameObject)
		{
			PlayerStatsComponent component = gameObject.GetComponent<PlayerStatsComponent>();
			if ((bool)component)
			{
				component.InstanceHandleStatsUpdate(netMsg.reader);
			}
		}
	}

	[Client]
	private void InstanceHandleStatsUpdate(NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.Stats.PlayerStatsComponent::InstanceHandleStatsUpdate(UnityEngine.Networking.NetworkReader)' called on server");
			return;
		}
		if (!NetworkServer.active)
		{
			currentStats.Read(reader);
		}
		FlushStatsToUserProfile();
	}

	[Client]
	private void FlushStatsToUserProfile()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.Stats.PlayerStatsComponent::FlushStatsToUserProfile()' called on server");
			return;
		}
		StatSheet.GetDelta(clientDeltaStatsBuffer, currentStats, recordedStats);
		currentStats.ClearHasChangedFlags();
		StatSheet.Copy(currentStats, recordedStats);
		(playerCharacterMasterController.networkUser?.localUser)?.userProfile?.ApplyDeltaStatSheet(clientDeltaStatsBuffer);
	}

	[ConCommand(commandName = "print_stats", flags = ConVarFlags.None, helpText = "Prints all current stats of the sender.")]
	private static void CCPrintStats(ConCommandArgs args)
	{
		StatSheet statSheet = args.senderMasterObject?.GetComponent<PlayerStatsComponent>()?.currentStats;
		if (statSheet != null)
		{
			string[] array = new string[statSheet.fields.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = $"[\"{statSheet.fields[i].name}\"]={statSheet.fields[i].ToString()}";
			}
		}
	}

	private void UNetVersion()
	{
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
