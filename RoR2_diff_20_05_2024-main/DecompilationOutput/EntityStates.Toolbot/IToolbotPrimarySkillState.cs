using RoR2.Skills;
using UnityEngine;

namespace EntityStates.Toolbot;

public interface IToolbotPrimarySkillState : ISkillState
{
	Transform muzzleTransform { get; set; }

	string muzzleName { get; set; }

	string baseMuzzleName { get; }

	bool isInDualWield { get; set; }

	int currentHand { get; set; }

	SkillDef skillDef { get; set; }
}
