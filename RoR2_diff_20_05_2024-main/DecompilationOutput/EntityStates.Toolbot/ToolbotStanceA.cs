using RoR2;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class ToolbotStanceA : ToolbotStanceBase
{
	public override void OnEnter()
	{
		base.OnEnter();
		swapStateType = typeof(ToolbotStanceB);
		if (NetworkServer.active)
		{
			SetEquipmentSlot(0);
		}
	}

	protected override GenericSkill GetCurrentPrimarySkill()
	{
		return GetPrimarySkill1();
	}
}
