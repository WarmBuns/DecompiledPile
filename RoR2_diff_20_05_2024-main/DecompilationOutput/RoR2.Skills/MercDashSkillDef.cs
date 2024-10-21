using EntityStates;
using EntityStates.Merc;
using HG;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Skills;

[CreateAssetMenu(menuName = "RoR2/SkillDef/MercDashSkillDef")]
public class MercDashSkillDef : SkillDef
{
	protected class InstanceData : BaseSkillInstanceData
	{
		public int currentDashIndex;

		public float timeoutTimer;

		public bool waitingForHit;

		public bool hasExtraStock;
	}

	public int maxDashes;

	public float timeoutDuration;

	public Sprite[] icons;

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
		if (instanceData.waitingForHit && skillSlot.stateMachine.state is Assaulter2 { grantAnotherDash: not false })
		{
			instanceData.waitingForHit = false;
			AddHit(skillSlot);
		}
		instanceData.timeoutTimer -= deltaTime;
		if (instanceData.timeoutTimer <= 0f && instanceData.currentDashIndex != 0)
		{
			if (instanceData.hasExtraStock)
			{
				skillSlot.stock--;
				instanceData.hasExtraStock = false;
			}
			instanceData.currentDashIndex = 0;
		}
	}

	protected override EntityState InstantiateNextState([NotNull] GenericSkill skillSlot)
	{
		EntityState entityState = base.InstantiateNextState(skillSlot);
		if (entityState is PrepAssaulter2 prepAssaulter)
		{
			prepAssaulter.dashIndex = ((InstanceData)skillSlot.skillInstanceData).currentDashIndex;
		}
		return entityState;
	}

	public override void OnExecute([NotNull] GenericSkill skillSlot)
	{
		base.OnExecute(skillSlot);
		InstanceData instanceData = (InstanceData)skillSlot.skillInstanceData;
		if (!instanceData.hasExtraStock)
		{
			instanceData.currentDashIndex = 0;
		}
		instanceData.waitingForHit = true;
		instanceData.hasExtraStock = false;
		instanceData.timeoutTimer = timeoutDuration;
	}

	protected void AddHit([NotNull] GenericSkill skillSlot)
	{
		InstanceData instanceData = (InstanceData)skillSlot.skillInstanceData;
		if (instanceData.currentDashIndex < maxDashes - 1)
		{
			instanceData.currentDashIndex++;
			int stock = skillSlot.stock + 1;
			skillSlot.stock = stock;
			instanceData.hasExtraStock = true;
		}
		else
		{
			instanceData.currentDashIndex = 0;
		}
	}

	public override Sprite GetCurrentIcon([NotNull] GenericSkill skillSlot)
	{
		int index = ((InstanceData)skillSlot.skillInstanceData)?.currentDashIndex ?? 0;
		return ArrayUtils.GetSafe(icons, index);
	}
}
