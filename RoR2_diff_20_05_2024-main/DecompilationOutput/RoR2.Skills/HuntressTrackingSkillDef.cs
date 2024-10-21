using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Skills;

[CreateAssetMenu(menuName = "RoR2/SkillDef/HuntressTrackingSkillDef")]
public class HuntressTrackingSkillDef : SkillDef
{
	protected class InstanceData : BaseSkillInstanceData
	{
		public HuntressTracker huntressTracker;
	}

	public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
	{
		return new InstanceData
		{
			huntressTracker = skillSlot.GetComponent<HuntressTracker>()
		};
	}

	private static bool HasTarget([NotNull] GenericSkill skillSlot)
	{
		if (!(((InstanceData)skillSlot.skillInstanceData).huntressTracker?.GetTrackingTarget()))
		{
			return false;
		}
		return true;
	}

	public override bool CanExecute([NotNull] GenericSkill skillSlot)
	{
		if (!HasTarget(skillSlot))
		{
			return false;
		}
		return base.CanExecute(skillSlot);
	}

	public override bool IsReady([NotNull] GenericSkill skillSlot)
	{
		if (base.IsReady(skillSlot))
		{
			return HasTarget(skillSlot);
		}
		return false;
	}
}
