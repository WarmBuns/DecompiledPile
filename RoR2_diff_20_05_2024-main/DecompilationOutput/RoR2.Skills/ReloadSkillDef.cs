using EntityStates;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Skills;

[CreateAssetMenu(menuName = "RoR2/SkillDef/ReloadSkillDef")]
public class ReloadSkillDef : SkillDef
{
	protected class InstanceData : BaseSkillInstanceData
	{
		public int currentStock;

		public float graceStopwatch;
	}

	[Header("Reload Parameters")]
	[Tooltip("The reload state to go into, when stock is less than max.")]
	public SerializableEntityStateType reloadState;

	[Tooltip("The priority of this reload state.")]
	public InterruptPriority reloadInterruptPriority = InterruptPriority.Skill;

	[Tooltip("The amount of time to wait between when we COULD reload, and when we actually start")]
	public float graceDuration;

	public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
	{
		return new InstanceData();
	}

	public override void OnUnassigned([NotNull] GenericSkill skillSlot)
	{
		base.OnUnassigned(skillSlot);
	}

	public override void OnFixedUpdate([NotNull] GenericSkill skillSlot, float deltaTime)
	{
		base.OnFixedUpdate(skillSlot, deltaTime);
		InstanceData instanceData = (InstanceData)skillSlot.skillInstanceData;
		instanceData.currentStock = skillSlot.stock;
		if (instanceData.currentStock >= GetMaxStock(skillSlot))
		{
			return;
		}
		if ((bool)skillSlot.stateMachine && !skillSlot.stateMachine.HasPendingState() && skillSlot.stateMachine.CanInterruptState(reloadInterruptPriority))
		{
			instanceData.graceStopwatch += deltaTime;
			if (instanceData.graceStopwatch >= graceDuration || instanceData.currentStock == 0)
			{
				skillSlot.stateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref reloadState));
			}
		}
		else
		{
			instanceData.graceStopwatch = 0f;
		}
	}

	public override void OnExecute([NotNull] GenericSkill skillSlot)
	{
		base.OnExecute(skillSlot);
		((InstanceData)skillSlot.skillInstanceData).currentStock = skillSlot.stock;
	}
}
