using RoR2.Skills;
using UnityEngine;

namespace EntityStates.Toolbot;

public abstract class BaseToolbotPrimarySkillState : BaseSkillState, IToolbotPrimarySkillState, ISkillState
{
	public Transform muzzleTransform { get; set; }

	public virtual string baseMuzzleName => "Muzzle";

	public bool isInDualWield { get; set; }

	public int currentHand { get; set; }

	public string muzzleName { get; set; }

	public SkillDef skillDef { get; set; }

	public override void OnEnter()
	{
		base.OnEnter();
		BaseToolbotPrimarySkillStateMethods.OnEnter(this, base.gameObject, base.skillLocator, GetModelChildLocator());
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
