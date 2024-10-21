using System.Collections.Generic;
using EntityStates.Engi.MineDeployer;

namespace RoR2.Skills;

public class EngiMineDeployerSkill : SkillDef
{
	public override bool CanExecute(GenericSkill skillSlot)
	{
		List<BaseMineDeployerState> instancesList = BaseMineDeployerState.instancesList;
		for (int i = 0; i < instancesList.Count; i++)
		{
			if (instancesList[i].owner == skillSlot.gameObject)
			{
				return false;
			}
		}
		return base.CanExecute(skillSlot);
	}
}
