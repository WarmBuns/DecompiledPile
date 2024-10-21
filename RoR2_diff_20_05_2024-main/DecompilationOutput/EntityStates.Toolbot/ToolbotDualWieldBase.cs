using System;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public abstract class ToolbotDualWieldBase : GenericCharacterMain, ISkillState
{
	public static BuffDef penaltyBuff;

	public static BuffDef bonusBuff;

	public static SkillDef inertSkillDef;

	public static SkillDef cancelSkillDef;

	public static CharacterCameraParams cameraParams;

	[SerializeField]
	public GameObject crosshairOverridePrefab;

	[SerializeField]
	public bool applyPenaltyBuff = true;

	[SerializeField]
	public bool applyBonusBuff = true;

	[SerializeField]
	public bool applyCameraAimMode = true;

	private GenericSkill specialSlot;

	private bool allowPrimarySkills;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private CameraTargetParams.CameraParamsOverrideHandle cameraParamsOverrideHandle;

	private static int isDualWieldingParamHash = Animator.StringToHash("isDualWielding");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	private static GenericSkill.StateMachineResolver offhandStateMachineResolverDelegate = OffhandStateMachineResolver;

	public GenericSkill activatorSkillSlot { get; set; }

	protected GenericSkill primary1Slot { get; private set; }

	protected GenericSkill primary2Slot { get; private set; }

	protected virtual bool shouldAllowPrimarySkills => false;

	public override void OnEnter()
	{
		base.OnEnter();
		allowPrimarySkills = shouldAllowPrimarySkills;
		if (NetworkServer.active && (bool)base.characterBody)
		{
			if ((bool)penaltyBuff && applyPenaltyBuff)
			{
				base.characterBody.AddBuff(penaltyBuff);
			}
			if ((bool)bonusBuff && applyBonusBuff)
			{
				base.characterBody.AddBuff(bonusBuff);
			}
		}
		if ((bool)base.skillLocator)
		{
			primary1Slot = base.skillLocator.FindSkillByFamilyName("ToolbotBodyPrimary1");
			primary2Slot = base.skillLocator.FindSkillByFamilyName("ToolbotBodyPrimary2");
			specialSlot = base.skillLocator.FindSkillByFamilyName("ToolbotBodySpecialFamily");
			if (!allowPrimarySkills)
			{
				if ((object)inertSkillDef != null)
				{
					if ((bool)base.skillLocator.primary)
					{
						base.skillLocator.primary.SetSkillOverride(this, inertSkillDef, GenericSkill.SkillOverridePriority.Contextual);
					}
					if ((bool)base.skillLocator.secondary)
					{
						base.skillLocator.secondary.SetSkillOverride(this, inertSkillDef, GenericSkill.SkillOverridePriority.Contextual);
					}
				}
			}
			else if ((bool)base.skillLocator.secondary && (bool)primary2Slot)
			{
				base.skillLocator.secondary.SetSkillOverride(this, primary2Slot.skillDef, GenericSkill.SkillOverridePriority.Contextual);
				base.skillLocator.secondary.customStateMachineResolver += offhandStateMachineResolverDelegate;
			}
			if ((bool)specialSlot && (object)cancelSkillDef != null)
			{
				specialSlot.SetSkillOverride(this, cancelSkillDef, GenericSkill.SkillOverridePriority.Contextual);
			}
		}
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
		if ((bool)base.modelAnimator)
		{
			base.modelAnimator.SetBool(isDualWieldingParamHash, value: true);
		}
		if ((bool)base.cameraTargetParams && applyCameraAimMode)
		{
			cameraParamsOverrideHandle = base.cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
			{
				cameraParamsData = cameraParams.data,
				priority = 1f
			});
		}
		StopSkills();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(1f);
			if (base.isAuthority && (base.characterBody.isSprinting || base.characterBody.HasBuff(DLC2Content.Buffs.DisableAllSkills.buffIndex)))
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)specialSlot && (object)cancelSkillDef != null)
		{
			specialSlot.UnsetSkillOverride(this, cancelSkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}
		if (!allowPrimarySkills)
		{
			if ((object)inertSkillDef != null)
			{
				if ((bool)base.skillLocator.secondary)
				{
					base.skillLocator.secondary.UnsetSkillOverride(this, inertSkillDef, GenericSkill.SkillOverridePriority.Contextual);
				}
				if ((bool)base.skillLocator.primary)
				{
					base.skillLocator.primary.UnsetSkillOverride(this, inertSkillDef, GenericSkill.SkillOverridePriority.Contextual);
				}
			}
		}
		else if ((bool)base.skillLocator.secondary && (bool)primary2Slot)
		{
			base.skillLocator.secondary.UnsetSkillOverride(this, primary2Slot.skillDef, GenericSkill.SkillOverridePriority.Contextual);
			base.skillLocator.secondary.customStateMachineResolver -= offhandStateMachineResolverDelegate;
		}
		if (NetworkServer.active && (bool)base.characterBody)
		{
			if ((bool)bonusBuff && applyBonusBuff)
			{
				base.characterBody.RemoveBuff(bonusBuff);
			}
			if ((bool)penaltyBuff && applyPenaltyBuff)
			{
				base.characterBody.RemoveBuff(penaltyBuff);
			}
		}
		crosshairOverrideRequest?.Dispose();
		if ((bool)base.modelAnimator)
		{
			base.modelAnimator.SetBool(isDualWieldingParamHash, value: false);
		}
		PlayAnimation("DualWield, Additive", EmptyStateHash);
		if ((bool)base.cameraTargetParams && cameraParamsOverrideHandle.isValid)
		{
			cameraParamsOverrideHandle = base.cameraTargetParams.RemoveParamsOverride(cameraParamsOverrideHandle);
		}
		base.OnExit();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		this.Serialize(base.skillLocator, writer);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		this.Deserialize(base.skillLocator, reader);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	private static void OffhandStateMachineResolver(GenericSkill genericSkill, SkillDef skillDef, ref EntityStateMachine targetStateMachine)
	{
		if (string.Equals(skillDef.activationStateMachineName, "Weapon", StringComparison.Ordinal))
		{
			targetStateMachine = EntityStateMachine.FindByCustomName(genericSkill.gameObject, "Weapon2");
		}
	}

	protected void StopSkills()
	{
		if (base.isAuthority)
		{
			EntityStateMachine.FindByCustomName(base.gameObject, "Weapon")?.SetNextStateToMain();
			EntityStateMachine.FindByCustomName(base.gameObject, "Weapon2")?.SetNextStateToMain();
		}
	}
}
