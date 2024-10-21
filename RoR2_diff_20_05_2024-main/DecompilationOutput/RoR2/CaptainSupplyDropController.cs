using System.Runtime.InteropServices;
using EntityStates.SurvivorPod;
using JetBrains.Annotations;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class CaptainSupplyDropController : NetworkBehaviour
{
	[Header("Referenced Components")]
	public GenericSkill orbitalStrikeSkill;

	public GenericSkill prepSupplyDropSkill;

	public GenericSkill supplyDrop1Skill;

	public GenericSkill supplyDrop2Skill;

	[Header("Skill Defs")]
	public SkillDef usedUpSkillDef;

	public SkillDef lostConnectionSkillDef;

	[SyncVar]
	private byte netEnabledSkillsMask;

	private byte authorityEnabledSkillsMask;

	private bool attemptSupplyDropReset;

	private CharacterBody characterBody;

	[CanBeNull]
	private SkillDef currentSupplyDrop1SkillDef;

	[CanBeNull]
	private SkillDef currentSupplyDrop2SkillDef;

	[CanBeNull]
	private SkillDef currentPrepSupplyDropSkillDef;

	[CanBeNull]
	private SkillDef currentOrbitalStrikeSkillDef;

	private static int kCmdCmdSetSkillMask;

	private bool canUseOrbitalSkills
	{
		get
		{
			if (SceneCatalog.mostRecentSceneDef.sceneType != SceneType.Stage)
			{
				return SceneCatalog.mostRecentSceneDef.sceneType == SceneType.UntimedStage;
			}
			return true;
		}
	}

	private bool hasEffectiveAuthority => characterBody.hasEffectiveAuthority;

	public byte NetworknetEnabledSkillsMask
	{
		get
		{
			return netEnabledSkillsMask;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref netEnabledSkillsMask, 1u);
		}
	}

	private void Awake()
	{
		characterBody = GetComponent<CharacterBody>();
		attemptSupplyDropReset = (bool)supplyDrop1Skill || (bool)supplyDrop2Skill;
		Release.onEnter += OnPodRelease;
	}

	private void OnPodRelease(Release state, CharacterBody body)
	{
		if ((object)body == characterBody && base.hasAuthority)
		{
			CheckSkillsForReset();
			if ((bool)supplyDrop1Skill && supplyDrop1Skill.stock == 0)
			{
				Debug.LogError($"SupplyDrop1Skill's got no stocks, max stock = {supplyDrop1Skill.maxStock}.  Forcing one stock");
				supplyDrop1Skill.AddOneStock();
			}
			if ((bool)supplyDrop2Skill && supplyDrop2Skill.stock == 0)
			{
				Debug.LogError($"SupplyDrop2Skill's got no stocks, max stock = {supplyDrop2Skill.maxStock}.  Forcing one stock");
				supplyDrop2Skill.AddOneStock();
			}
		}
	}

	private void FixedUpdate()
	{
		UpdateSkillOverrides();
		if (hasEffectiveAuthority && attemptSupplyDropReset)
		{
			CheckSkillsForReset();
			attemptSupplyDropReset = (!supplyDrop1Skill || supplyDrop1Skill.stock == 0) && (!supplyDrop2Skill || supplyDrop2Skill.stock == 0);
			_ = attemptSupplyDropReset;
		}
	}

	private void CheckSkillsForReset()
	{
		if ((bool)supplyDrop1Skill && supplyDrop1Skill.stock == 0)
		{
			if (supplyDrop1Skill.maxStock <= 0)
			{
				Debug.LogError($"SupplyDrop1Skill's max stock = {supplyDrop1Skill.maxStock}.  Recalculating...");
				supplyDrop1Skill.RecalculateValues();
			}
			if (supplyDrop1Skill.maxStock > 0)
			{
				supplyDrop1Skill.Reset();
			}
		}
		if ((bool)supplyDrop2Skill && supplyDrop2Skill.stock == 0)
		{
			if (supplyDrop2Skill.maxStock <= 0)
			{
				Debug.LogError($"SupplyDrop2Skill's max stock = {supplyDrop2Skill.maxStock}.  Recalculating...");
				supplyDrop2Skill.RecalculateValues();
			}
			if (supplyDrop2Skill.maxStock > 0)
			{
				supplyDrop2Skill.Reset();
			}
		}
	}

	private void OnDisable()
	{
		SetSkillOverride(ref currentSupplyDrop1SkillDef, null, supplyDrop1Skill);
		SetSkillOverride(ref currentSupplyDrop2SkillDef, null, supplyDrop2Skill);
	}

	private void SetSkillOverride([CanBeNull] ref SkillDef currentSkillDef, [CanBeNull] SkillDef newSkillDef, [NotNull] GenericSkill component)
	{
		if ((object)currentSkillDef != newSkillDef)
		{
			if ((object)currentSkillDef != null)
			{
				component.UnsetSkillOverride(this, currentSkillDef, GenericSkill.SkillOverridePriority.Contextual);
			}
			currentSkillDef = newSkillDef;
			if ((object)currentSkillDef != null)
			{
				component.SetSkillOverride(this, currentSkillDef, GenericSkill.SkillOverridePriority.Contextual);
			}
		}
	}

	private void UpdateSkillOverrides()
	{
		if (!base.enabled)
		{
			return;
		}
		byte b = 0;
		if (hasEffectiveAuthority)
		{
			byte b2 = 0;
			if (supplyDrop1Skill.stock > 0 || !supplyDrop1Skill.skillDef)
			{
				b2 = (byte)(b2 | 1u);
			}
			if (supplyDrop2Skill.stock > 0 || !supplyDrop2Skill.skillDef)
			{
				b2 = (byte)(b2 | 2u);
			}
			if (b2 != authorityEnabledSkillsMask)
			{
				authorityEnabledSkillsMask = b2;
				if (NetworkServer.active)
				{
					NetworknetEnabledSkillsMask = authorityEnabledSkillsMask;
				}
				else
				{
					CallCmdSetSkillMask(authorityEnabledSkillsMask);
				}
			}
			b = authorityEnabledSkillsMask;
		}
		else
		{
			b = netEnabledSkillsMask;
		}
		bool flag = (b & 1) != 0;
		bool flag2 = (b & 2) != 0;
		SetSkillOverride(ref currentSupplyDrop1SkillDef, flag ? null : usedUpSkillDef, supplyDrop1Skill);
		SetSkillOverride(ref currentSupplyDrop2SkillDef, flag2 ? null : usedUpSkillDef, supplyDrop2Skill);
	}

	[Command]
	private void CmdSetSkillMask(byte newMask)
	{
		NetworknetEnabledSkillsMask = newMask;
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdSetSkillMask(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetSkillMask called on client.");
		}
		else
		{
			((CaptainSupplyDropController)obj).CmdSetSkillMask((byte)reader.ReadPackedUInt32());
		}
	}

	public void CallCmdSetSkillMask(byte newMask)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetSkillMask called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetSkillMask(newMask);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetSkillMask);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32(newMask);
		SendCommandInternal(networkWriter, 0, "CmdSetSkillMask");
	}

	static CaptainSupplyDropController()
	{
		kCmdCmdSetSkillMask = 176967897;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CaptainSupplyDropController), kCmdCmdSetSkillMask, InvokeCmdCmdSetSkillMask);
		NetworkCRC.RegisterBehaviour("CaptainSupplyDropController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32(netEnabledSkillsMask);
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
			writer.WritePackedUInt32(netEnabledSkillsMask);
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
			netEnabledSkillsMask = (byte)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			netEnabledSkillsMask = (byte)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
