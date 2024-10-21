using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2.Skills;

public class CaptainSupplyDropSkillDef : CaptainOrbitalSkillDef
{
	protected class InstanceData : BaseSkillInstanceData, IDisposable
	{
		public List<GenericSkill> supplySkillSlots;

		public bool anySupplyDropsAvailable;

		private GenericSkill skillSlot;

		private string[] supplyDropSkillSlotNames;

		public InstanceData(GenericSkill skillSlot, string[] supplyDropSkillSlotNames)
		{
			supplySkillSlots = CollectionPool<GenericSkill, List<GenericSkill>>.RentCollection();
			this.skillSlot = skillSlot;
			this.supplyDropSkillSlotNames = supplyDropSkillSlotNames;
		}

		public void Dispose()
		{
			supplySkillSlots = CollectionPool<GenericSkill, List<GenericSkill>>.ReturnCollection(supplySkillSlots);
			skillSlot = null;
			supplyDropSkillSlotNames = null;
		}

		private bool CheckForSupplyDropAvailable()
		{
			for (int i = 0; i < supplySkillSlots.Count; i++)
			{
				if (supplySkillSlots[i].IsReady())
				{
					return true;
				}
			}
			return false;
		}

		private void FindSupplyDropSkillSlots()
		{
			for (int i = 0; i < supplyDropSkillSlotNames.Length; i++)
			{
				GenericSkill genericSkill = skillSlot.GetComponent<SkillLocator>().FindSkill(supplyDropSkillSlotNames[i]);
				if ((bool)genericSkill)
				{
					supplySkillSlots.Add(genericSkill);
				}
			}
		}

		public void FixedUpdate()
		{
			if (supplySkillSlots.Count == 0)
			{
				FindSupplyDropSkillSlots();
			}
			anySupplyDropsAvailable = CheckForSupplyDropAvailable();
		}
	}

	public string[] supplyDropSkillSlotNames = Array.Empty<string>();

	public Sprite exhaustedIcon;

	public string exhaustedNameToken;

	public string exhaustedDescriptionToken;

	public override Sprite GetCurrentIcon(GenericSkill skillSlot)
	{
		if (!((InstanceData)skillSlot.skillInstanceData).anySupplyDropsAvailable)
		{
			return exhaustedIcon;
		}
		return base.GetCurrentIcon(skillSlot);
	}

	public override string GetCurrentNameToken(GenericSkill skillSlot)
	{
		if (!((InstanceData)skillSlot.skillInstanceData).anySupplyDropsAvailable)
		{
			return exhaustedNameToken;
		}
		return base.GetCurrentNameToken(skillSlot);
	}

	public override string GetCurrentDescriptionToken(GenericSkill skillSlot)
	{
		if (!((InstanceData)skillSlot.skillInstanceData).anySupplyDropsAvailable)
		{
			return exhaustedDescriptionToken;
		}
		return base.GetCurrentDescriptionToken(skillSlot);
	}

	public override bool CanExecute(GenericSkill skillSlot)
	{
		if (((InstanceData)skillSlot.skillInstanceData).anySupplyDropsAvailable)
		{
			return base.CanExecute(skillSlot);
		}
		return false;
	}

	public override bool IsReady(GenericSkill skillSlot)
	{
		if (((InstanceData)skillSlot.skillInstanceData).anySupplyDropsAvailable)
		{
			return base.IsReady(skillSlot);
		}
		return false;
	}

	public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
	{
		return new InstanceData(skillSlot, supplyDropSkillSlotNames);
	}

	public override void OnUnassigned(GenericSkill skillSlot)
	{
		((InstanceData)skillSlot.skillInstanceData).Dispose();
		base.OnUnassigned(skillSlot);
	}

	public override void OnFixedUpdate(GenericSkill skillSlot, float deltaTime)
	{
		base.OnFixedUpdate(skillSlot, deltaTime);
		((InstanceData)skillSlot.skillInstanceData).FixedUpdate();
	}
}
