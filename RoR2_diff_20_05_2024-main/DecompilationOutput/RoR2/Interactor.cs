using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class Interactor : NetworkBehaviour
{
	public float maxInteractionDistance = 1f;

	private static int kCmdCmdInteract;

	private static int kRpcRpcInteractionResult;

	public GameObject FindBestInteractableObject(Ray raycastRay, float maxRaycastDistance, Vector3 overlapPosition, float overlapRadius)
	{
		LayerMask interactable = LayerIndex.CommonMasks.interactable;
		if (Physics.Raycast(raycastRay, out var hitInfo, maxRaycastDistance, interactable, QueryTriggerInteraction.Collide) && EntityLocator.HasEntityLocator(hitInfo.collider.gameObject, out var foundLocator))
		{
			GameObject entity = foundLocator.entity;
			IInteractable component = entity.GetComponent<IInteractable>();
			if (component != null && ((MonoBehaviour)component).isActiveAndEnabled && component.GetInteractability(this) != 0)
			{
				entity.GetComponent<ISupportsMultipleInspectZones>()?.SetInspectInfoProvider(foundLocator.EntityZoneIndex);
				return entity;
			}
		}
		Collider[] colliders;
		int num = HGPhysics.OverlapSphere(out colliders, overlapPosition, overlapRadius, interactable, QueryTriggerInteraction.Collide);
		GameObject gameObject = null;
		EntityLocator entityLocator = null;
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			Collider collider = colliders[i];
			if (!EntityLocator.HasEntityLocator(collider.gameObject, out var foundLocator2))
			{
				continue;
			}
			GameObject entity2 = foundLocator2.entity;
			IInteractable component2 = entity2.GetComponent<IInteractable>();
			if (component2 != null && ((MonoBehaviour)component2).isActiveAndEnabled && component2.GetInteractability(this) != 0 && !component2.ShouldIgnoreSpherecastForInteractibility(this))
			{
				float num3 = Vector3.Dot((collider.transform.position - overlapPosition).normalized, raycastRay.direction);
				if (num3 > num2)
				{
					num2 = num3;
					gameObject = entity2.gameObject;
					entityLocator = foundLocator2;
				}
			}
		}
		HGPhysics.ReturnResults(colliders);
		if ((bool)gameObject)
		{
			gameObject.GetComponent<ISupportsMultipleInspectZones>()?.SetInspectInfoProvider(entityLocator.EntityZoneIndex);
		}
		return gameObject;
	}

	[Command]
	public void CmdInteract(GameObject interactableObject)
	{
		PerformInteraction(interactableObject);
	}

	[Server]
	private void PerformInteraction(GameObject interactableObject)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Interactor::PerformInteraction(UnityEngine.GameObject)' called on client");
		}
		else
		{
			if (!interactableObject)
			{
				return;
			}
			bool flag = false;
			bool anyInteractionSucceeded = false;
			IInteractable[] components = interactableObject.GetComponents<IInteractable>();
			foreach (IInteractable interactable in components)
			{
				Interactability interactability = interactable.GetInteractability(this);
				if (interactability == Interactability.Available)
				{
					interactable.OnInteractionBegin(this);
					GlobalEventManager.instance.OnInteractionBegin(this, interactable, interactableObject);
					anyInteractionSucceeded = true;
				}
				flag = flag || interactability != Interactability.Disabled;
			}
			if (flag)
			{
				CallRpcInteractionResult(anyInteractionSucceeded);
			}
		}
	}

	[ClientRpc]
	private void RpcInteractionResult(bool anyInteractionSucceeded)
	{
		if (!anyInteractionSucceeded && CameraRigController.IsObjectSpectatedByAnyCamera(base.gameObject))
		{
			Util.PlaySound("Play_UI_insufficient_funds", RoR2Application.instance.gameObject);
		}
	}

	public void AttemptInteraction(GameObject interactableObject)
	{
		if (NetworkServer.active)
		{
			PerformInteraction(interactableObject);
		}
		else
		{
			CallCmdInteract(interactableObject);
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdInteract(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdInteract called on client.");
		}
		else
		{
			((Interactor)obj).CmdInteract(reader.ReadGameObject());
		}
	}

	public void CallCmdInteract(GameObject interactableObject)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdInteract called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdInteract(interactableObject);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdInteract);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(interactableObject);
		SendCommandInternal(networkWriter, 0, "CmdInteract");
	}

	protected static void InvokeRpcRpcInteractionResult(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcInteractionResult called on server.");
		}
		else
		{
			((Interactor)obj).RpcInteractionResult(reader.ReadBoolean());
		}
	}

	public void CallRpcInteractionResult(bool anyInteractionSucceeded)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcInteractionResult called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcInteractionResult);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(anyInteractionSucceeded);
		SendRPCInternal(networkWriter, 0, "RpcInteractionResult");
	}

	static Interactor()
	{
		kCmdCmdInteract = 591229007;
		NetworkBehaviour.RegisterCommandDelegate(typeof(Interactor), kCmdCmdInteract, InvokeCmdCmdInteract);
		kRpcRpcInteractionResult = 804118976;
		NetworkBehaviour.RegisterRpcDelegate(typeof(Interactor), kRpcRpcInteractionResult, InvokeRpcRpcInteractionResult);
		NetworkCRC.RegisterBehaviour("Interactor", 0);
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
