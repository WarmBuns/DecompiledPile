using System;
using HG;
using RoR2.Networking;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

[RequireComponent(typeof(NetworkIdentity))]
public class SkillLocator : NetworkBehaviour
{
	[Serializable]
	public struct PassiveSkill
	{
		public bool enabled;

		public string skillNameToken;

		public string skillDescriptionToken;

		public string keywordToken;

		public Sprite icon;
	}

	[FormerlySerializedAs("skill1")]
	public GenericSkill primary;

	[FormerlySerializedAs("skill2")]
	public GenericSkill secondary;

	[FormerlySerializedAs("skill3")]
	public GenericSkill utility;

	[FormerlySerializedAs("skill4")]
	public GenericSkill special;

	public PassiveSkill passiveSkill;

	[SerializeField]
	private GenericSkill primaryBonusStockOverrideSkill;

	[SerializeField]
	private GenericSkill secondaryBonusStockOverrideSkill;

	[SerializeField]
	private GenericSkill utilityBonusStockOverrideSkill;

	[SerializeField]
	private GenericSkill specialBonusStockOverrideSkill;

	private NetworkIdentity networkIdentity;

	private GenericSkill[] allSkills;

	private bool hasEffectiveAuthority;

	private uint skillDefDirtyFlags;

	private bool inDeserialize;

	private static int kRpcRpcDeductCooldownFromAllSkillsServer;

	public GenericSkill primaryBonusStockSkill
	{
		get
		{
			if (!primaryBonusStockOverrideSkill)
			{
				return primary;
			}
			return primaryBonusStockOverrideSkill;
		}
	}

	public GenericSkill secondaryBonusStockSkill
	{
		get
		{
			if (!secondaryBonusStockOverrideSkill)
			{
				return secondary;
			}
			return secondaryBonusStockOverrideSkill;
		}
	}

	public GenericSkill utilityBonusStockSkill
	{
		get
		{
			if (!utilityBonusStockOverrideSkill)
			{
				return utility;
			}
			return utilityBonusStockOverrideSkill;
		}
	}

	public GenericSkill specialBonusStockSkill
	{
		get
		{
			if (!specialBonusStockOverrideSkill)
			{
				return special;
			}
			return specialBonusStockOverrideSkill;
		}
	}

	public int skillSlotCount => allSkills.Length;

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		allSkills = GetComponents<GenericSkill>();
	}

	private void Start()
	{
		UpdateAuthority();
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		base.OnStopAuthority();
		UpdateAuthority();
	}

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(networkIdentity);
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			for (int i = 0; i < allSkills.Length; i++)
			{
				GenericSkill genericSkill = allSkills[i];
				if (genericSkill.baseSkill != genericSkill.defaultSkillDef)
				{
					num |= (uint)(1 << i);
				}
			}
		}
		writer.WritePackedUInt32(num);
		for (int j = 0; j < allSkills.Length; j++)
		{
			if ((num & (uint)(1 << j)) != 0)
			{
				GenericSkill genericSkill2 = allSkills[j];
				writer.WritePackedUInt32((uint)((genericSkill2.baseSkill?.skillIndex ?? (-1)) + 1));
			}
		}
		return num != 0;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		inDeserialize = true;
		uint num = reader.ReadPackedUInt32();
		for (int i = 0; i < allSkills.Length; i++)
		{
			if ((num & (uint)(1 << i)) != 0)
			{
				GenericSkill genericSkill = allSkills[i];
				SkillDef skillDef = SkillCatalog.GetSkillDef((int)(reader.ReadPackedUInt32() - 1));
				if (initialState || !hasEffectiveAuthority)
				{
					genericSkill.SetBaseSkill(skillDef);
				}
			}
		}
		inDeserialize = false;
	}

	public GenericSkill FindSkill(string skillName)
	{
		for (int i = 0; i < allSkills.Length; i++)
		{
			if (allSkills[i].skillName == skillName)
			{
				return allSkills[i];
			}
		}
		return null;
	}

	public GenericSkill FindSkillByFamilyName(string skillFamilyName)
	{
		for (int i = 0; i < allSkills.Length; i++)
		{
			if (SkillCatalog.GetSkillFamilyName(allSkills[i].skillFamily.catalogIndex) == skillFamilyName)
			{
				return allSkills[i];
			}
		}
		return null;
	}

	public GenericSkill FindSkillByDef(SkillDef skillDef)
	{
		for (int i = 0; i < allSkills.Length; i++)
		{
			if ((object)allSkills[i].skillDef == skillDef)
			{
				return allSkills[i];
			}
		}
		return null;
	}

	public GenericSkill GetSkill(SkillSlot skillSlot)
	{
		return skillSlot switch
		{
			SkillSlot.Primary => primary, 
			SkillSlot.Secondary => secondary, 
			SkillSlot.Utility => utility, 
			SkillSlot.Special => special, 
			_ => null, 
		};
	}

	public GenericSkill GetSkillAtIndex(int index)
	{
		return ArrayUtils.GetSafe(allSkills, index);
	}

	public int GetSkillSlotIndex(GenericSkill skillSlot)
	{
		return Array.IndexOf(allSkills, skillSlot);
	}

	public SkillSlot FindSkillSlot(GenericSkill skillComponent)
	{
		if (!skillComponent)
		{
			return SkillSlot.None;
		}
		if (skillComponent == primary)
		{
			return SkillSlot.Primary;
		}
		if (skillComponent == secondary)
		{
			return SkillSlot.Secondary;
		}
		if (skillComponent == utility)
		{
			return SkillSlot.Utility;
		}
		if (skillComponent == special)
		{
			return SkillSlot.Special;
		}
		return SkillSlot.None;
	}

	[Server]
	public void ApplyLoadoutServer(Loadout loadout, BodyIndex bodyIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SkillLocator::ApplyLoadoutServer(RoR2.Loadout,RoR2.BodyIndex)' called on client");
		}
		else
		{
			if (bodyIndex == BodyIndex.None)
			{
				return;
			}
			for (int i = 0; i < allSkills.Length; i++)
			{
				uint num = loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, i);
				GenericSkill obj = allSkills[i];
				SkillFamily.Variant[] variants = obj.skillFamily.variants;
				if (!ArrayUtils.IsInBounds(variants, num))
				{
					num = 0u;
				}
				obj.SetBaseSkill(variants[num].skillDef);
			}
		}
	}

	public void ResetSkills()
	{
		if (NetworkServer.active && networkIdentity.clientAuthorityOwner != null)
		{
			NetworkWriter networkWriter = new NetworkWriter();
			networkWriter.StartMessage(56);
			networkWriter.Write(base.gameObject);
			networkWriter.FinishMessage();
			networkIdentity.clientAuthorityOwner.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
		}
		for (int i = 0; i < allSkills.Length; i++)
		{
			allSkills[i].Reset();
		}
	}

	public void ApplyAmmoPack()
	{
		if (NetworkServer.active && !networkIdentity.hasAuthority)
		{
			NetworkWriter networkWriter = new NetworkWriter();
			networkWriter.StartMessage(63);
			networkWriter.Write(base.gameObject);
			networkWriter.FinishMessage();
			networkIdentity.clientAuthorityOwner?.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
			return;
		}
		GenericSkill[] array = allSkills;
		foreach (GenericSkill genericSkill in array)
		{
			if (genericSkill.CanApplyAmmoPack())
			{
				genericSkill.ApplyAmmoPack();
			}
		}
	}

	[NetworkMessageHandler(msgType = 56, client = true)]
	private static void HandleResetSkills(NetworkMessage netMsg)
	{
		GameObject gameObject = netMsg.reader.ReadGameObject();
		if (!NetworkServer.active && (bool)gameObject)
		{
			SkillLocator component = gameObject.GetComponent<SkillLocator>();
			if ((bool)component)
			{
				component.ResetSkills();
			}
		}
	}

	[NetworkMessageHandler(msgType = 63, client = true)]
	private static void HandleAmmoPackPickup(NetworkMessage netMsg)
	{
		GameObject gameObject = netMsg.reader.ReadGameObject();
		if (!NetworkServer.active && (bool)gameObject)
		{
			SkillLocator component = gameObject.GetComponent<SkillLocator>();
			if ((bool)component)
			{
				component.ApplyAmmoPack();
			}
		}
	}

	public void DeductCooldownFromAllSkillsServer(float deduction)
	{
		if (hasEffectiveAuthority)
		{
			DeductCooldownFromAllSkillsAuthority(deduction);
		}
		else
		{
			CallRpcDeductCooldownFromAllSkillsServer(deduction);
		}
	}

	[ClientRpc]
	private void RpcDeductCooldownFromAllSkillsServer(float deduction)
	{
		if (hasEffectiveAuthority)
		{
			DeductCooldownFromAllSkillsAuthority(deduction);
		}
	}

	private void DeductCooldownFromAllSkillsAuthority(float deduction)
	{
		for (int i = 0; i < allSkills.Length; i++)
		{
			GenericSkill genericSkill = allSkills[i];
			if (genericSkill.stock < genericSkill.maxStock)
			{
				genericSkill.rechargeStopwatch += deduction;
			}
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcDeductCooldownFromAllSkillsServer(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDeductCooldownFromAllSkillsServer called on server.");
		}
		else
		{
			((SkillLocator)obj).RpcDeductCooldownFromAllSkillsServer(reader.ReadSingle());
		}
	}

	public void CallRpcDeductCooldownFromAllSkillsServer(float deduction)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcDeductCooldownFromAllSkillsServer called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcDeductCooldownFromAllSkillsServer);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(deduction);
		SendRPCInternal(networkWriter, 0, "RpcDeductCooldownFromAllSkillsServer");
	}

	static SkillLocator()
	{
		kRpcRpcDeductCooldownFromAllSkillsServer = -2090076365;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SkillLocator), kRpcRpcDeductCooldownFromAllSkillsServer, InvokeRpcRpcDeductCooldownFromAllSkillsServer);
		NetworkCRC.RegisterBehaviour("SkillLocator", 0);
	}

	public override void PreStartClient()
	{
	}
}
