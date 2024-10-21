using EntityStates;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Skills;

public class SteppedSkillDef : SkillDef
{
	public class InstanceData : BaseSkillInstanceData
	{
		public int step;
	}

	public interface IStepSetter
	{
		void SetStep(int i);
	}

	public int stepCount = 2;

	[Tooltip("The amount of time a step is 'held' before it resets. Only begins to count down when available to execute.")]
	public float stepGraceDuration = 0.1f;

	private float stepResetTimer;

	public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
	{
		return new InstanceData();
	}

	protected override EntityState InstantiateNextState([NotNull] GenericSkill skillSlot)
	{
		EntityState entityState = base.InstantiateNextState(skillSlot);
		InstanceData instanceData = (InstanceData)skillSlot.skillInstanceData;
		if (entityState is IStepSetter stepSetter)
		{
			stepSetter.SetStep(instanceData.step);
		}
		return entityState;
	}

	public override void OnExecute([NotNull] GenericSkill skillSlot)
	{
		base.OnExecute(skillSlot);
		InstanceData instanceData = (InstanceData)skillSlot.skillInstanceData;
		instanceData.step++;
		if (instanceData.step >= stepCount)
		{
			instanceData.step = 0;
		}
	}

	public override void OnFixedUpdate([NotNull] GenericSkill skillSlot, float deltaTime)
	{
		base.OnFixedUpdate(skillSlot, deltaTime);
		if (skillSlot.CanExecute())
		{
			stepResetTimer += deltaTime;
		}
		else
		{
			stepResetTimer = 0f;
		}
		if (stepResetTimer > stepGraceDuration)
		{
			((InstanceData)skillSlot.skillInstanceData).step = 0;
		}
	}
}
