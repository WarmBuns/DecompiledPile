using RoR2;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class ToolbotStanceB : ToolbotStanceBase
{
	public override void OnEnter()
	{
		base.OnEnter();
		swapStateType = typeof(ToolbotStanceA);
		if (NetworkServer.active)
		{
			SetEquipmentSlot(1);
		}
	}

	protected override GenericSkill GetCurrentPrimarySkill()
	{
		return GetPrimarySkill2();
	}
}
