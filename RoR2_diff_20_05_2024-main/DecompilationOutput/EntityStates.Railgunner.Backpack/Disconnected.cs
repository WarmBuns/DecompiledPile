using RoR2.Skills;
using UnityEngine;

namespace EntityStates.Railgunner.Backpack;

public class Disconnected : BaseBackpack
{
	[SerializeField]
	public SkillDef superSkillDef;

	[SerializeField]
	public SkillDef cryoSkillDef;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((object)base.skillLocator.special.skillDef == superSkillDef)
		{
			outer.SetNextState(new OnlineSuper());
		}
		else if ((object)base.skillLocator.special.skillDef == cryoSkillDef)
		{
			outer.SetNextState(new OnlineCryo());
		}
	}
}
