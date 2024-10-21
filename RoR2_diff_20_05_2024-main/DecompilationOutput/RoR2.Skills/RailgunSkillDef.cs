using EntityStates.Railgunner.Backpack;
using EntityStates.Railgunner.Reload;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Skills;

[CreateAssetMenu(menuName = "RoR2/SkillDef/RailgunSkillDef")]
public class RailgunSkillDef : SkillDef
{
	private class InstanceData : BaseSkillInstanceData
	{
		public EntityStateMachine backpackStateMachine;

		public EntityStateMachine reloadStateMachine;
	}

	private const string backpackStateMachineName = "Backpack";

	private const string reloadStateMachineName = "Reload";

	[SerializeField]
	public Sprite offlineIcon;

	[SerializeField]
	public bool restockOnReload;

	public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
	{
		return new InstanceData
		{
			backpackStateMachine = EntityStateMachine.FindByCustomName(skillSlot.gameObject, "Backpack"),
			reloadStateMachine = EntityStateMachine.FindByCustomName(skillSlot.gameObject, "Reload")
		};
	}

	private bool IsBackpackOffline([NotNull] GenericSkill skillSlot)
	{
		if (skillSlot.skillInstanceData is InstanceData instanceData && (bool)instanceData.backpackStateMachine)
		{
			return instanceData.backpackStateMachine.state is Offline;
		}
		return false;
	}

	private bool IsReloading([NotNull] GenericSkill skillSlot)
	{
		if (skillSlot.skillInstanceData is InstanceData instanceData && (bool)instanceData.reloadStateMachine)
		{
			return instanceData.reloadStateMachine.state is Reloading;
		}
		return false;
	}

	public override bool IsReady([NotNull] GenericSkill skillSlot)
	{
		if (base.IsReady(skillSlot) && !IsBackpackOffline(skillSlot))
		{
			return !IsReloading(skillSlot);
		}
		return false;
	}

	public override void OnFixedUpdate([NotNull] GenericSkill skillSlot, float deltaTime)
	{
		base.OnFixedUpdate(skillSlot, deltaTime);
		if (IsBackpackOffline(skillSlot))
		{
			skillSlot.rechargeStopwatch = 0f;
		}
	}

	public override Sprite GetCurrentIcon([NotNull] GenericSkill skillSlot)
	{
		if (IsBackpackOffline(skillSlot))
		{
			return offlineIcon;
		}
		return base.GetCurrentIcon(skillSlot);
	}
}
