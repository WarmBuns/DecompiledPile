using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Skills;

[CreateAssetMenu(menuName = "RoR2/SkillDef/PassiveItem")]
public class PassiveItemSkillDef : SkillDef
{
	private class InstanceData : BaseSkillInstanceData
	{
		public bool hasItemBeenGiven;
	}

	[SerializeField]
	public ItemDef passiveItem;

	public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
	{
		InstanceData instanceData = new InstanceData
		{
			hasItemBeenGiven = false
		};
		if (NetworkServer.active)
		{
			TryGiveItem(skillSlot, instanceData);
		}
		return instanceData;
	}

	public override void OnFixedUpdate(GenericSkill skillSlot, float deltaTime)
	{
		if (NetworkServer.active)
		{
			TryGiveItem(skillSlot, (InstanceData)skillSlot.skillInstanceData);
		}
	}

	public override void OnUnassigned(GenericSkill skillSlot)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		InstanceData instanceData = (InstanceData)skillSlot.skillInstanceData;
		if (instanceData.hasItemBeenGiven)
		{
			CharacterBody component = skillSlot.GetComponent<CharacterBody>();
			if ((bool)component && (bool)component.inventory)
			{
				component.inventory.RemoveItem(passiveItem);
				instanceData.hasItemBeenGiven = false;
			}
		}
	}

	private void TryGiveItem(GenericSkill skillSlot, InstanceData data)
	{
		if (!data.hasItemBeenGiven)
		{
			CharacterBody component = skillSlot.GetComponent<CharacterBody>();
			if ((bool)component && (bool)component.inventory)
			{
				component.inventory.GiveItem(passiveItem);
				data.hasItemBeenGiven = true;
			}
		}
	}
}
