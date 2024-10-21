using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RoR2.UI;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class PingerController : NetworkBehaviour
{
	[Serializable]
	public struct PingInfo : IEquatable<PingInfo>
	{
		public bool active;

		public Vector3 origin;

		public Vector3 normal;

		public NetworkIdentity targetNetworkIdentity;

		public GameObject targetGameObject
		{
			get
			{
				if (!targetNetworkIdentity)
				{
					return null;
				}
				return targetNetworkIdentity.gameObject;
			}
		}

		public bool Equals(PingInfo other)
		{
			if (active.Equals(other.active) && origin.Equals(other.origin) && normal.Equals(other.normal))
			{
				return (object)targetNetworkIdentity == other.targetNetworkIdentity;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			return (obj as PingInfo?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
		{
			return (((-1814869148 * -1521134295 + active.GetHashCode()) * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(origin)) * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(normal)) * -1521134295 + EqualityComparer<NetworkIdentity>.Default.GetHashCode(targetNetworkIdentity);
		}
	}

	private int pingStock = 3;

	private float pingRechargeStopwatch;

	private const int maximumPingStock = 3;

	private const float pingRechargeInterval = 1.5f;

	private static readonly PingInfo emptyPing;

	private PingIndicator pingIndicator;

	[SyncVar(hook = "OnSyncCurrentPing")]
	public PingInfo currentPing;

	private static int kCmdCmdPing;

	public PingInfo NetworkcurrentPing
	{
		get
		{
			return currentPing;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncCurrentPing(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref currentPing, 1u);
		}
	}

	private void RebuildPing(PingInfo pingInfo)
	{
		if (!pingInfo.active && (object)pingIndicator != null)
		{
			if ((bool)pingIndicator)
			{
				UnityEngine.Object.Destroy(pingIndicator.gameObject);
			}
			pingIndicator = null;
			return;
		}
		if (!pingIndicator)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/PingIndicator"));
			pingIndicator = gameObject.GetComponent<PingIndicator>();
			pingIndicator.pingOwner = base.gameObject;
		}
		pingIndicator.pingOrigin = pingInfo.origin;
		pingIndicator.pingNormal = pingInfo.normal;
		pingIndicator.pingTarget = pingInfo.targetGameObject;
		pingIndicator.RebuildPing();
	}

	private void OnDestroy()
	{
		if ((bool)pingIndicator)
		{
			UnityEngine.Object.Destroy(pingIndicator.gameObject);
		}
	}

	private void OnSyncCurrentPing(PingInfo newPingInfo)
	{
		if (!base.hasAuthority)
		{
			SetCurrentPing(newPingInfo);
		}
	}

	private void SetCurrentPing(PingInfo newPingInfo)
	{
		if ((bool)currentPing.targetGameObject)
		{
			currentPing.targetGameObject.GetComponent<TeleporterInteraction>()?.CancelTeleporterPing(pingIndicator);
		}
		NetworkcurrentPing = newPingInfo;
		RebuildPing(currentPing);
		if (base.hasAuthority)
		{
			CallCmdPing(currentPing);
		}
	}

	[Command]
	private void CmdPing(PingInfo incomingPing)
	{
		NetworkcurrentPing = incomingPing;
	}

	private void FixedUpdate()
	{
		if (base.hasAuthority)
		{
			pingRechargeStopwatch -= Time.fixedDeltaTime;
			if (pingRechargeStopwatch <= 0f)
			{
				pingStock = Mathf.Min(pingStock + 1, 3);
				pingRechargeStopwatch = 1.5f;
			}
		}
	}

	private static bool GeneratePingInfo(Ray aimRay, GameObject bodyObject, out PingInfo result)
	{
		result = new PingInfo
		{
			active = true,
			origin = Vector3.zero,
			normal = Vector3.zero,
			targetNetworkIdentity = null
		};
		aimRay = CameraRigController.ModifyAimRayIfApplicable(aimRay, bodyObject, out var extraRaycastDistance);
		float maxDistance = 1000f + extraRaycastDistance;
		if (Util.CharacterRaycast(bodyObject, aimRay, out var hitInfo, maxDistance, (int)LayerIndex.entityPrecise.mask | (int)LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal))
		{
			HurtBox component = hitInfo.collider.GetComponent<HurtBox>();
			if ((bool)component && (bool)component.healthComponent)
			{
				CharacterBody body = component.healthComponent.body;
				result.origin = body.corePosition;
				result.normal = Vector3.zero;
				result.targetNetworkIdentity = body.networkIdentity;
				return true;
			}
		}
		if (Util.CharacterRaycast(bodyObject, aimRay, out hitInfo, maxDistance, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault | (int)LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
		{
			GameObject gameObject = hitInfo.collider.gameObject;
			NetworkIdentity networkIdentity = gameObject.GetComponentInParent<NetworkIdentity>();
			ForcePingable component2 = gameObject.GetComponent<ForcePingable>();
			if (!networkIdentity && (component2 == null || !component2.bypassEntityLocator))
			{
				Transform parent = gameObject.transform.parent;
				EntityLocator entityLocator = (parent ? parent.GetComponentInChildren<EntityLocator>() : gameObject.GetComponent<EntityLocator>());
				if ((bool)entityLocator)
				{
					gameObject = entityLocator.entity;
					networkIdentity = gameObject.GetComponent<NetworkIdentity>();
				}
			}
			result.origin = hitInfo.point;
			result.normal = hitInfo.normal;
			if ((bool)networkIdentity && networkIdentity.isPingable)
			{
				result.targetNetworkIdentity = networkIdentity;
			}
			return true;
		}
		return false;
	}

	public void AttemptPing(Ray aimRay, GameObject bodyObject)
	{
		if (pingStock <= 0)
		{
			Chat.AddMessage(Language.GetString("PLAYER_PING_COOLDOWN"));
			return;
		}
		if (!RoR2Application.isInSinglePlayer)
		{
			pingStock--;
		}
		if (!GeneratePingInfo(aimRay, bodyObject, out var result))
		{
			return;
		}
		if ((object)result.targetNetworkIdentity != null && (object)result.targetNetworkIdentity == currentPing.targetNetworkIdentity)
		{
			if ((bool)currentPing.targetGameObject && (bool)currentPing.targetGameObject.GetComponent<TeleporterInteraction>())
			{
				if ((bool)pingIndicator)
				{
					result = emptyPing;
					pingStock++;
				}
			}
			else
			{
				result = emptyPing;
				pingStock++;
			}
		}
		SetCurrentPing(result);
	}

	static PingerController()
	{
		kCmdCmdPing = 1170265357;
		NetworkBehaviour.RegisterCommandDelegate(typeof(PingerController), kCmdCmdPing, InvokeCmdCmdPing);
		NetworkCRC.RegisterBehaviour("PingerController", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdPing(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdPing called on client.");
		}
		else
		{
			((PingerController)obj).CmdPing(GeneratedNetworkCode._ReadPingInfo_PingerController(reader));
		}
	}

	public void CallCmdPing(PingInfo incomingPing)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdPing called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdPing(incomingPing);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdPing);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WritePingInfo_PingerController(networkWriter, incomingPing);
		SendCommandInternal(networkWriter, 0, "CmdPing");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WritePingInfo_PingerController(writer, currentPing);
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
			GeneratedNetworkCode._WritePingInfo_PingerController(writer, currentPing);
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
			currentPing = GeneratedNetworkCode._ReadPingInfo_PingerController(reader);
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncCurrentPing(GeneratedNetworkCode._ReadPingInfo_PingerController(reader));
		}
	}

	public override void PreStartClient()
	{
	}
}
