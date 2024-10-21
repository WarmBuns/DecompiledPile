using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkedBodyAttachment))]
public class LunarDetonatorPassiveAttachment : NetworkBehaviour, INetworkedBodyAttachmentListener
{
	private class DamageListener : MonoBehaviour, IOnDamageDealtServerReceiver
	{
		public LunarDetonatorPassiveAttachment passiveController;

		public void OnDamageDealtServer(DamageReport damageReport)
		{
			if (passiveController.skillAvailable && damageReport.victim.alive && Util.CheckRoll(damageReport.damageInfo.procCoefficient * 100f, damageReport.attackerMaster))
			{
				damageReport.victimBody.AddTimedBuff(RoR2Content.Buffs.LunarDetonationCharge, 10f);
			}
		}
	}

	private GenericSkill _monitoredSkill;

	[SyncVar(hook = "SetSkillSlotIndexPlusOne")]
	private uint skillSlotIndexPlusOne;

	private bool skillAvailable;

	private NetworkedBodyAttachment networkedBodyAttachment;

	private DamageListener damageListener;

	private static int kCmdCmdSetSkillAvailable;

	public GenericSkill monitoredSkill
	{
		get
		{
			return _monitoredSkill;
		}
		set
		{
			if ((object)_monitoredSkill == value)
			{
				return;
			}
			_monitoredSkill = value;
			int num = -1;
			if ((bool)_monitoredSkill)
			{
				SkillLocator component = _monitoredSkill.GetComponent<SkillLocator>();
				if ((bool)component)
				{
					num = component.GetSkillSlotIndex(_monitoredSkill);
				}
			}
			SetSkillSlotIndexPlusOne((uint)(num + 1));
		}
	}

	public uint NetworkskillSlotIndexPlusOne
	{
		get
		{
			return skillSlotIndexPlusOne;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetSkillSlotIndexPlusOne(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref skillSlotIndexPlusOne, 1u);
		}
	}

	private void Awake()
	{
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
	}

	private void FixedUpdate()
	{
		if (networkedBodyAttachment.hasEffectiveAuthority)
		{
			FixedUpdateAuthority();
		}
	}

	private void OnDestroy()
	{
		if ((bool)damageListener)
		{
			Object.Destroy(damageListener);
		}
		damageListener = null;
	}

	public override void OnStartClient()
	{
		SetSkillSlotIndexPlusOne(skillSlotIndexPlusOne);
	}

	private void SetSkillSlotIndexPlusOne(uint newSkillSlotIndexPlusOne)
	{
		NetworkskillSlotIndexPlusOne = newSkillSlotIndexPlusOne;
		if (!NetworkServer.active)
		{
			ResolveMonitoredSkill();
		}
	}

	private void ResolveMonitoredSkill()
	{
		if ((bool)networkedBodyAttachment.attachedBody)
		{
			SkillLocator component = networkedBodyAttachment.attachedBody.GetComponent<SkillLocator>();
			if ((bool)component)
			{
				monitoredSkill = component.GetSkillAtIndex((int)(skillSlotIndexPlusOne - 1));
			}
		}
	}

	private void FixedUpdateAuthority()
	{
		bool flag = false;
		if ((bool)monitoredSkill)
		{
			flag = monitoredSkill.stock > 0;
		}
		if (skillAvailable != flag)
		{
			skillAvailable = flag;
			if (!NetworkServer.active)
			{
				CallCmdSetSkillAvailable(skillAvailable);
			}
		}
	}

	[Command]
	private void CmdSetSkillAvailable(bool newSkillAvailable)
	{
		skillAvailable = newSkillAvailable;
	}

	public void OnAttachedBodyDiscovered(NetworkedBodyAttachment networkedBodyAttachment, CharacterBody attachedBody)
	{
		if (NetworkServer.active)
		{
			damageListener = attachedBody.gameObject.AddComponent<DamageListener>();
			damageListener.passiveController = this;
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdSetSkillAvailable(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetSkillAvailable called on client.");
		}
		else
		{
			((LunarDetonatorPassiveAttachment)obj).CmdSetSkillAvailable(reader.ReadBoolean());
		}
	}

	public void CallCmdSetSkillAvailable(bool newSkillAvailable)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetSkillAvailable called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetSkillAvailable(newSkillAvailable);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetSkillAvailable);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(newSkillAvailable);
		SendCommandInternal(networkWriter, 0, "CmdSetSkillAvailable");
	}

	static LunarDetonatorPassiveAttachment()
	{
		kCmdCmdSetSkillAvailable = -1453655134;
		NetworkBehaviour.RegisterCommandDelegate(typeof(LunarDetonatorPassiveAttachment), kCmdCmdSetSkillAvailable, InvokeCmdCmdSetSkillAvailable);
		NetworkCRC.RegisterBehaviour("LunarDetonatorPassiveAttachment", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32(skillSlotIndexPlusOne);
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
			writer.WritePackedUInt32(skillSlotIndexPlusOne);
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
			skillSlotIndexPlusOne = reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetSkillSlotIndexPlusOne(reader.ReadPackedUInt32());
		}
	}

	public override void PreStartClient()
	{
	}
}
