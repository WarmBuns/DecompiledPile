using RoR2;
using UnityEngine.Networking;

namespace EntityStates.ImpMonster.Weapon;

public class CloakPermanent : BaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterBody && NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.Cloak);
		}
	}

	public override void OnExit()
	{
		if ((bool)base.characterBody && NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
