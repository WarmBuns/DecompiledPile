using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Skills;

[CreateAssetMenu(menuName = "RoR2/SkillDef/VoidRaidCrabBodySkillDef")]
public class VoidRaidCrabBodySkillDef : SkillDef
{
	private class InstanceData : BaseSkillInstanceData
	{
		public EntityStateMachine weaponStateMachine;
	}

	private const string weaponStateMachineName = "Weapon";

	public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
	{
		return new InstanceData
		{
			weaponStateMachine = EntityStateMachine.FindByCustomName(skillSlot.gameObject, "Weapon")
		};
	}

	public override bool IsReady([NotNull] GenericSkill skillSlot)
	{
		if (base.IsReady(skillSlot))
		{
			return !IsWeaponBusy(skillSlot);
		}
		return false;
	}

	private bool IsWeaponBusy([NotNull] GenericSkill skillSlot)
	{
		if (skillSlot.skillInstanceData is InstanceData instanceData && (bool)instanceData.weaponStateMachine)
		{
			return !instanceData.weaponStateMachine.IsInMainState();
		}
		return false;
	}
}
