using System.Collections.Generic;
using EntityStates.Railgunner.Backpack;
using RoR2;
using RoR2.Skills;

namespace EntityStates.Railgunner.Reload;

public class Waiting : EntityState
{
	private const string backpackStateMachineName = "Backpack";

	private const string scopeStateMachineName = "Scope";

	private List<GenericSkill> restockOnReloadList = new List<GenericSkill>();

	private EntityStateMachine backpackStateMachine;

	private EntityStateMachine scopeStateMachine;

	private bool isReloadQueued;

	public Waiting()
	{
		isReloadQueued = false;
	}

	public Waiting(bool queueReload)
	{
		isReloadQueued = queueReload;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.skillLocator)
		{
			for (int i = 0; i < base.skillLocator.skillSlotCount; i++)
			{
				GenericSkill skillAtIndex = base.skillLocator.GetSkillAtIndex(i);
				if ((bool)skillAtIndex)
				{
					skillAtIndex.onSkillChanged += OnSkillChanged;
				}
			}
			ReevaluateSkills();
		}
		backpackStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Backpack");
		scopeStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Scope");
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!isReloadQueued)
		{
			foreach (GenericSkill restockOnReload in restockOnReloadList)
			{
				if (restockOnReload.stock == 0)
				{
					isReloadQueued = true;
					break;
				}
			}
		}
		if (isReloadQueued && CanReload())
		{
			outer.SetNextState(new Reloading());
		}
	}

	public override void OnExit()
	{
		if ((bool)base.skillLocator)
		{
			for (int i = 0; i < base.skillLocator.skillSlotCount; i++)
			{
				GenericSkill skillAtIndex = base.skillLocator.GetSkillAtIndex(i);
				if ((bool)skillAtIndex)
				{
					skillAtIndex.onSkillChanged -= OnSkillChanged;
				}
			}
		}
		base.OnExit();
	}

	public bool CanReload()
	{
		if (!(backpackStateMachine.state is Offline))
		{
			return scopeStateMachine.IsInMainState();
		}
		return false;
	}

	public void QueueReload()
	{
		isReloadQueued = true;
	}

	private void OnSkillChanged(GenericSkill skill)
	{
		ReevaluateSkills();
	}

	private void ReevaluateSkills()
	{
		restockOnReloadList.Clear();
		for (int i = 0; i < base.skillLocator.skillSlotCount; i++)
		{
			GenericSkill skillAtIndex = base.skillLocator.GetSkillAtIndex(i);
			if ((bool)skillAtIndex)
			{
				RailgunSkillDef railgunSkillDef = skillAtIndex.skillDef as RailgunSkillDef;
				if ((bool)railgunSkillDef && railgunSkillDef.restockOnReload)
				{
					restockOnReloadList.Add(skillAtIndex);
				}
			}
		}
	}
}
