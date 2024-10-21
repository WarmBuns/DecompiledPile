using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.VoidRaidCrab;

public class VacuumExit : BaseVacuumAttackState
{
	[SerializeField]
	public SkillDef skillDefToReplaceAtStocksEmpty;

	[SerializeField]
	public SkillDef nextSkillDef;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)nextSkillDef)
		{
			GenericSkill genericSkill = base.skillLocator.FindSkillByDef(skillDefToReplaceAtStocksEmpty);
			if ((bool)genericSkill && genericSkill.stock == 0)
			{
				genericSkill.SetBaseSkill(nextSkillDef);
			}
		}
	}

	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextStateToMain();
	}
}
