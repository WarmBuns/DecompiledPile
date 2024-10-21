using System;
using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class NetworkParent : NetworkBehaviour
{
	[Serializable]
	private struct ParentIdentifier : IEquatable<ParentIdentifier>
	{
		public byte indexInParentChildLocatorPlusOne;

		public NetworkInstanceId parentNetworkInstanceId;

		public int indexInParentChildLocator
		{
			get
			{
				return indexInParentChildLocatorPlusOne - 1;
			}
			set
			{
				indexInParentChildLocatorPlusOne = (byte)(value + 1);
			}
		}

		private static ChildLocator LookUpChildLocator(Transform rootObject)
		{
			ModelLocator component = rootObject.GetComponent<ModelLocator>();
			if (!component)
			{
				return null;
			}
			Transform modelTransform = component.modelTransform;
			if (!modelTransform)
			{
				return null;
			}
			return modelTransform.GetComponent<ChildLocator>();
		}

		public ParentIdentifier(Transform parent)
		{
			parentNetworkInstanceId = NetworkInstanceId.Invalid;
			indexInParentChildLocatorPlusOne = 0;
			if (!parent)
			{
				return;
			}
			NetworkIdentity componentInParent = parent.GetComponentInParent<NetworkIdentity>();
			if (!componentInParent)
			{
				Debug.LogWarningFormat("NetworkParent cannot accept a non-null parent without a NetworkIdentity! parent={0}", parent);
				return;
			}
			parentNetworkInstanceId = componentInParent.netId;
			if ((object)componentInParent.gameObject == parent.gameObject)
			{
				return;
			}
			ChildLocator childLocator = LookUpChildLocator(componentInParent.transform);
			if (!childLocator)
			{
				Debug.LogWarningFormat("NetworkParent can only be parented directly to another object with a NetworkIdentity or an object registered in the ChildLocator of a a model of an object with a NetworkIdentity. parent={0}", parent);
				return;
			}
			indexInParentChildLocator = childLocator.FindChildIndex(parent);
			if (indexInParentChildLocatorPlusOne == 0)
			{
				Debug.LogWarningFormat("NetworkParent parent={0} is not registered in a ChildLocator.", parent);
			}
		}

		public bool Equals(ParentIdentifier other)
		{
			if (indexInParentChildLocatorPlusOne == other.indexInParentChildLocatorPlusOne)
			{
				return parentNetworkInstanceId.Equals(other.parentNetworkInstanceId);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is ParentIdentifier other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (indexInParentChildLocatorPlusOne.GetHashCode() * 397) ^ parentNetworkInstanceId.GetHashCode();
		}

		public Transform Resolve()
		{
			GameObject gameObject = Util.FindNetworkObject(parentNetworkInstanceId);
			NetworkIdentity networkIdentity = (gameObject ? gameObject.GetComponent<NetworkIdentity>() : null);
			if (!networkIdentity)
			{
				return null;
			}
			if (indexInParentChildLocatorPlusOne == 0)
			{
				return networkIdentity.transform;
			}
			ChildLocator childLocator = LookUpChildLocator(networkIdentity.transform);
			if ((bool)childLocator)
			{
				return childLocator.FindChild(indexInParentChildLocator);
			}
			return null;
		}
	}

	private Transform cachedServerParentTransform;

	private new Transform transform;

	[SyncVar(hook = "SetParentIdentifier")]
	private ParentIdentifier parentIdentifier;

	public ParentIdentifier NetworkparentIdentifier
	{
		get
		{
			return parentIdentifier;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetParentIdentifier(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref parentIdentifier, 1u);
		}
	}

	private void Awake()
	{
		transform = base.transform;
	}

	public override void OnStartServer()
	{
		ServerUpdateParent();
	}

	private void OnTransformParentChanged()
	{
		if (NetworkServer.active)
		{
			ServerUpdateParent();
		}
		if ((bool)transform.parent)
		{
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}
	}

	[Server]
	private void ServerUpdateParent()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkParent::ServerUpdateParent()' called on client");
			return;
		}
		Transform transform = this.transform.parent;
		if ((object)transform != cachedServerParentTransform)
		{
			if (!transform)
			{
				transform = null;
			}
			cachedServerParentTransform = transform;
			SetParentIdentifier(new ParentIdentifier(transform));
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SetParentIdentifier(parentIdentifier);
	}

	private void SetParentIdentifier(ParentIdentifier newParentIdentifier)
	{
		NetworkparentIdentifier = newParentIdentifier;
		if (!NetworkServer.active)
		{
			transform.parent = parentIdentifier.Resolve();
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WriteParentIdentifier_NetworkParent(writer, parentIdentifier);
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
			GeneratedNetworkCode._WriteParentIdentifier_NetworkParent(writer, parentIdentifier);
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
			parentIdentifier = GeneratedNetworkCode._ReadParentIdentifier_NetworkParent(reader);
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetParentIdentifier(GeneratedNetworkCode._ReadParentIdentifier_NetworkParent(reader));
		}
	}

	public override void PreStartClient()
	{
	}
}
