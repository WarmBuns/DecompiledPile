using EntityStates;
using EntityStates.VoidSurvivor.Weapon;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Skills;

public class VoidSurvivorBlasterSkillDef : SteppedSkillDef
{
	public Sprite[] icons;

	protected override EntityState InstantiateNextState([NotNull] GenericSkill skillSlot)
	{
		EntityState entityState = base.InstantiateNextState(skillSlot);
		VoidSurvivorController component = skillSlot.GetComponent<VoidSurvivorController>();
		if ((bool)component)
		{
			float corruptionPercentage = component.corruptionPercentage;
			entityState = ((corruptionPercentage >= 75f) ? new FireBlaster4() : ((corruptionPercentage >= 50f) ? new FireBlaster3() : ((!(corruptionPercentage >= 25f)) ? ((FireBlasterBase)new FireBlaster1()) : ((FireBlasterBase)new FireBlaster2()))));
		}
		InstanceData instanceData = (InstanceData)skillSlot.skillInstanceData;
		if (entityState is IStepSetter stepSetter)
		{
			stepSetter.SetStep(instanceData.step);
		}
		return entityState;
	}
}
