using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DisallowMultipleComponent]
public class MinionOwnership : NetworkBehaviour
{
	public class MinionGroup : IDisposable
	{
		private class MinionGroupDestroyer : MonoBehaviour
		{
			public MinionGroup group;

			private void OnDestroy()
			{
				group.Dispose();
				group.refCount--;
			}
		}

		private static readonly List<MinionGroup> instancesList = new List<MinionGroup>();

		public readonly NetworkInstanceId ownerId;

		private MinionOwnership[] _members;

		private int _memberCount;

		private int refCount;

		private bool resolved;

		private GameObject resolvedOwnerGameObject;

		private CharacterMaster resolvedOwnerMaster;

		public MinionOwnership[] members => _members;

		public int memberCount => _memberCount;

		public bool isMinion => ownerId != NetworkInstanceId.Invalid;

		public static MinionGroup FindGroup(NetworkInstanceId ownerId)
		{
			foreach (MinionGroup instances in instancesList)
			{
				if (instances.ownerId == ownerId)
				{
					return instances;
				}
			}
			return null;
		}

		public static void SetMinionOwner(MinionOwnership minion, NetworkInstanceId ownerId)
		{
			if (minion.group != null)
			{
				if (minion.group.ownerId == ownerId)
				{
					return;
				}
				RemoveMinion(minion.group.ownerId, minion);
			}
			if (ownerId != NetworkInstanceId.Invalid)
			{
				AddMinion(ownerId, minion);
			}
		}

		private static void AddMinion(NetworkInstanceId ownerId, MinionOwnership minion)
		{
			MinionGroup minionGroup = null;
			for (int i = 0; i < instancesList.Count; i++)
			{
				MinionGroup minionGroup2 = instancesList[i];
				if (instancesList[i].ownerId == ownerId)
				{
					minionGroup = minionGroup2;
					break;
				}
			}
			if (minionGroup == null)
			{
				minionGroup = new MinionGroup(ownerId);
			}
			minionGroup.AddMember(minion);
			minionGroup.AttemptToResolveOwner();
			CharacterMaster component = minion.GetComponent<CharacterMaster>();
			if ((bool)component)
			{
				component.inventory.GiveItem(RoR2Content.Items.MinionLeash);
			}
		}

		private static void RemoveMinion(NetworkInstanceId ownerId, MinionOwnership minion)
		{
			CharacterMaster component = minion.GetComponent<CharacterMaster>();
			if ((bool)component)
			{
				component.inventory.RemoveItem(RoR2Content.Items.MinionLeash);
			}
			MinionGroup minionGroup = null;
			for (int i = 0; i < instancesList.Count; i++)
			{
				MinionGroup minionGroup2 = instancesList[i];
				if (instancesList[i].ownerId == ownerId)
				{
					minionGroup = minionGroup2;
					break;
				}
			}
			if (minionGroup == null)
			{
				throw new InvalidOperationException(string.Format("{0}.{1} Could not find group to which {2} belongs", "MinionGroup", "RemoveMinion", minion));
			}
			minionGroup.RemoveMember(minion);
			if (minionGroup.refCount == 0 && !minionGroup.resolvedOwnerGameObject)
			{
				minionGroup.Dispose();
			}
		}

		private MinionGroup(NetworkInstanceId ownerId)
		{
			this.ownerId = ownerId;
			_members = new MinionOwnership[4];
			_memberCount = 0;
			instancesList.Add(this);
		}

		public void Dispose()
		{
			for (int num = _memberCount - 1; num >= 0; num--)
			{
				RemoveMemberAt(num);
			}
			instancesList.Remove(this);
		}

		private void AttemptToResolveOwner()
		{
			if (resolved)
			{
				return;
			}
			resolvedOwnerGameObject = Util.FindNetworkObject(ownerId);
			if ((bool)resolvedOwnerGameObject)
			{
				resolved = true;
				resolvedOwnerMaster = resolvedOwnerGameObject.GetComponent<CharacterMaster>();
				resolvedOwnerGameObject.AddComponent<MinionGroupDestroyer>().group = this;
				refCount++;
				for (int i = 0; i < _memberCount; i++)
				{
					_members[i].HandleOwnerDiscovery(resolvedOwnerMaster);
				}
			}
		}

		public void AddMember(MinionOwnership minion)
		{
			ArrayUtils.ArrayAppend(ref _members, ref _memberCount, in minion);
			refCount++;
			minion.HandleGroupDiscovery(this);
			if ((bool)resolvedOwnerMaster)
			{
				minion.HandleOwnerDiscovery(resolvedOwnerMaster);
			}
		}

		public void RemoveMember(MinionOwnership minion)
		{
			RemoveMemberAt(Array.IndexOf(_members, minion));
			refCount--;
		}

		private void RemoveMemberAt(int i)
		{
			MinionOwnership obj = _members[i];
			ArrayUtils.ArrayRemoveAt(_members, ref _memberCount, i);
			obj.HandleOwnerDiscovery(null);
			obj.HandleGroupDiscovery(null);
		}

		[ConCommand(commandName = "minion_dump", flags = ConVarFlags.None, helpText = "Prints debug information about all active minion groups.")]
		private static void CCMinionPrint(ConCommandArgs args)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < instancesList.Count; i++)
			{
				MinionGroup minionGroup = instancesList[i];
				stringBuilder.Append("group [").Append(i).Append("] size=")
					.Append(minionGroup._memberCount)
					.Append(" id=")
					.Append(minionGroup.ownerId)
					.Append(" resolvedOwnerGameObject=")
					.Append(minionGroup.resolvedOwnerGameObject)
					.AppendLine();
				for (int j = 0; j < minionGroup._memberCount; j++)
				{
					stringBuilder.Append("  ").Append("[").Append(j)
						.Append("] member.name=")
						.Append(minionGroup._members[j].name)
						.AppendLine();
				}
			}
		}
	}

	[SyncVar(hook = "OnSyncOwnerMasterId")]
	private NetworkInstanceId ownerMasterId = NetworkInstanceId.Invalid;

	public CharacterMaster ownerMaster { get; private set; }

	public MinionGroup group { get; private set; }

	public NetworkInstanceId NetworkownerMasterId
	{
		get
		{
			return ownerMasterId;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncOwnerMasterId(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref ownerMasterId, 1u);
		}
	}

	public event Action<CharacterMaster> onOwnerDiscovered;

	public event Action<CharacterMaster> onOwnerLost;

	public static event Action<MinionOwnership> onMinionGroupChangedGlobal;

	public static event Action<MinionOwnership> onMinionOwnerChangedGlobal;

	[Server]
	public void SetOwner(CharacterMaster newOwnerMaster)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.MinionOwnership::SetOwner(RoR2.CharacterMaster)' called on client");
			return;
		}
		NetworkownerMasterId = (newOwnerMaster ? newOwnerMaster.netId : NetworkInstanceId.Invalid);
		MinionGroup.SetMinionOwner(this, ownerMasterId);
	}

	private void OnSyncOwnerMasterId(NetworkInstanceId newOwnerMasterId)
	{
		MinionGroup.SetMinionOwner(this, ownerMasterId);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (!NetworkServer.active)
		{
			MinionGroup.SetMinionOwner(this, ownerMasterId);
		}
	}

	private void HandleGroupDiscovery(MinionGroup newGroup)
	{
		group = newGroup;
		MinionOwnership.onMinionGroupChangedGlobal?.Invoke(this);
	}

	private void HandleOwnerDiscovery(CharacterMaster newOwner)
	{
		if ((object)ownerMaster != null)
		{
			this.onOwnerLost?.Invoke(ownerMaster);
		}
		ownerMaster = newOwner;
		if ((object)ownerMaster != null)
		{
			this.onOwnerDiscovered?.Invoke(ownerMaster);
		}
		MinionOwnership.onMinionOwnerChangedGlobal?.Invoke(this);
	}

	private void OnDestroy()
	{
		MinionGroup.SetMinionOwner(this, NetworkInstanceId.Invalid);
	}

	[AssetCheck(typeof(CharacterMaster))]
	private static void AddMinionOwnershipComponent(AssetCheckArgs args)
	{
		CharacterMaster characterMaster = args.asset as CharacterMaster;
		if (!characterMaster.GetComponent<MinionOwnership>())
		{
			characterMaster.gameObject.AddComponent<MinionOwnership>();
			args.UpdatePrefab();
		}
	}

	private void OnValidate()
	{
		if (GetComponents<MinionOwnership>().Length > 1)
		{
			Debug.LogError("Only one MinionOwnership is allowed per object!", this);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(ownerMasterId);
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
			writer.Write(ownerMasterId);
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
			ownerMasterId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncOwnerMasterId(reader.ReadNetworkId());
		}
	}

	public override void PreStartClient()
	{
	}
}
