using System.Collections.Generic;
using System.Runtime.InteropServices;
using HG;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public sealed class NetworkedBodyAttachment : NetworkBehaviour
{
	[SyncVar(hook = "OnSyncAttachedBodyObject")]
	private GameObject _attachedBodyObject;

	[SyncVar(hook = "OnSyncAttachedBodyChildName")]
	private string attachedBodyChildName;

	public bool shouldParentToAttachedBody = true;

	public bool forceHostAuthority;

	private NetworkIdentity networkIdentity;

	private CharacterBody attachmentBody;

	private bool attached;

	private NetworkInstanceId ____attachedBodyObjectNetId;

	public GameObject attachedBodyObject => _attachedBodyObject;

	public CharacterBody attachedBody { get; private set; }

	public bool hasEffectiveAuthority { get; private set; }

	public GameObject Network_attachedBodyObject
	{
		get
		{
			return _attachedBodyObject;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncAttachedBodyObject(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVarGameObject(value, ref _attachedBodyObject, 1u, ref ____attachedBodyObjectNetId);
		}
	}

	public string NetworkattachedBodyChildName
	{
		get
		{
			return attachedBodyChildName;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncAttachedBodyChildName(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref attachedBodyChildName, 2u);
		}
	}

	private void OnSyncAttachedBodyObject(GameObject value)
	{
		if (!NetworkServer.active)
		{
			Network_attachedBodyObject = value;
			OnAttachedBodyObjectAssigned();
		}
	}

	private void OnSyncAttachedBodyChildName(string newName)
	{
		NetworkattachedBodyChildName = newName;
		if (shouldParentToAttachedBody)
		{
			ParentToBody();
		}
	}

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		attachmentBody = GetComponent<CharacterBody>();
	}

	[Server]
	public void AttachToGameObjectAndSpawn([NotNull] GameObject newAttachedBodyObject, string attachedBodyChildName = null)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkedBodyAttachment::AttachToGameObjectAndSpawn(UnityEngine.GameObject,System.String)' called on client");
		}
		else if (attached)
		{
			Debug.LogErrorFormat("Can't attach object '{0}' to object '{1}', it's already been assigned to object '{2}'.", base.gameObject, newAttachedBodyObject, attachedBodyObject);
		}
		else if ((bool)newAttachedBodyObject)
		{
			NetworkIdentity component = newAttachedBodyObject.GetComponent<NetworkIdentity>();
			if (component.netId.Value == 0)
			{
				Debug.LogWarningFormat("Network Identity for object {0} has a zero netID. Attachment will fail over the network.", newAttachedBodyObject);
			}
			NetworkattachedBodyChildName = attachedBodyChildName;
			Network_attachedBodyObject = newAttachedBodyObject;
			OnAttachedBodyObjectAssigned();
			NetworkConnection networkConnection = null;
			networkConnection = component.clientAuthorityOwner;
			if (networkConnection == null || forceHostAuthority)
			{
				NetworkServer.Spawn(base.gameObject);
			}
			else
			{
				NetworkServer.SpawnWithClientAuthority(base.gameObject, networkConnection);
			}
		}
	}

	private void OnAttachedBodyObjectAssigned()
	{
		if (attached)
		{
			return;
		}
		attached = true;
		if ((bool)_attachedBodyObject)
		{
			attachedBody = _attachedBodyObject.GetComponent<CharacterBody>();
			if (shouldParentToAttachedBody)
			{
				ParentToBody();
			}
		}
		if (!attachedBody)
		{
			return;
		}
		List<INetworkedBodyAttachmentListener> list = CollectionPool<INetworkedBodyAttachmentListener, List<INetworkedBodyAttachmentListener>>.RentCollection();
		GetComponents(list);
		foreach (INetworkedBodyAttachmentListener item in list)
		{
			item.OnAttachedBodyDiscovered(this, attachedBody);
		}
		list = CollectionPool<INetworkedBodyAttachmentListener, List<INetworkedBodyAttachmentListener>>.ReturnCollection(list);
	}

	private void ParentToBody()
	{
		if (!_attachedBodyObject)
		{
			return;
		}
		Transform parent = _attachedBodyObject.transform;
		if (!string.IsNullOrEmpty(attachedBodyChildName))
		{
			ModelLocator component = _attachedBodyObject.GetComponent<ModelLocator>();
			if ((bool)component && (bool)component.modelTransform)
			{
				ChildLocator component2 = component.modelTransform.GetComponent<ChildLocator>();
				if ((bool)component2)
				{
					Transform transform = component2.FindChild(attachedBodyChildName);
					if ((bool)transform)
					{
						parent = transform;
					}
					else
					{
						transform = component2.FindChild("Root");
						if ((bool)transform)
						{
							parent = transform;
						}
					}
				}
			}
		}
		base.transform.SetParent(parent, worldPositionStays: false);
		base.transform.localPosition = Vector3.zero;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		OnSyncAttachedBodyObject(attachedBodyObject);
	}

	private void FixedUpdate()
	{
		if (!attachedBodyObject && NetworkServer.active)
		{
			if ((bool)attachmentBody && (bool)attachmentBody.healthComponent)
			{
				attachmentBody.healthComponent.Suicide();
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void OnValidate()
	{
		if (!GetComponent<NetworkIdentity>().localPlayerAuthority && !forceHostAuthority)
		{
			Debug.LogWarningFormat("NetworkedBodyAttachment: Object {0} NetworkIdentity needs localPlayerAuthority=true", base.gameObject.name);
		}
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	public static void FindBodyAttachments(CharacterBody body, List<NetworkedBodyAttachment> output)
	{
		foreach (NetworkedBodyAttachment instances in InstanceTracker.GetInstancesList<NetworkedBodyAttachment>())
		{
			if ((object)instances.attachedBody == body)
			{
				output.Add(instances);
			}
		}
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();
		hasEffectiveAuthority = Util.HasEffectiveAuthority(networkIdentity);
	}

	public override void OnStopAuthority()
	{
		base.OnStopAuthority();
		hasEffectiveAuthority = Util.HasEffectiveAuthority(networkIdentity);
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_attachedBodyObject);
			writer.Write(attachedBodyChildName);
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
			writer.Write(_attachedBodyObject);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(attachedBodyChildName);
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
			____attachedBodyObjectNetId = reader.ReadNetworkId();
			attachedBodyChildName = reader.ReadString();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncAttachedBodyObject(reader.ReadGameObject());
		}
		if (((uint)num & 2u) != 0)
		{
			OnSyncAttachedBodyChildName(reader.ReadString());
		}
	}

	public override void PreStartClient()
	{
		if (!____attachedBodyObjectNetId.IsEmpty())
		{
			Network_attachedBodyObject = ClientScene.FindLocalObject(____attachedBodyObjectNetId);
		}
	}
}
