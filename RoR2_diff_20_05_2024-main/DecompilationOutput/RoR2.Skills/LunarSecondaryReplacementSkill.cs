namespace RoR2.Skills;

public class LunarSecondaryReplacementSkill : SkillDef
{
	private class InstanceData : BaseSkillInstanceData
	{
		public GenericSkill skillSlot;

		public void OnInventoryChanged()
		{
			skillSlot.RecalculateValues();
		}
	}

	public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
	{
		InstanceData instanceData = new InstanceData();
		instanceData.skillSlot = skillSlot;
		skillSlot.characterBody.onInventoryChanged += instanceData.OnInventoryChanged;
		return instanceData;
	}

	public override void OnUnassigned(GenericSkill skillSlot)
	{
		skillSlot.characterBody.onInventoryChanged -= ((InstanceData)skillSlot.skillInstanceData).OnInventoryChanged;
	}

	public override float GetRechargeInterval(GenericSkill skillSlot)
	{
		return (float)skillSlot.characterBody.inventory.GetItemCount(RoR2Content.Items.LunarSecondaryReplacement) * baseRechargeInterval;
	}
}
