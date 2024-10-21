using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.VoidRaidCrab;

public class SpinBeamExit : BaseSpinBeamAttackState
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

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= base.duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
