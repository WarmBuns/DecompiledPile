using RoR2;
using UnityEngine.Networking;

namespace EntityStates.RoboBallBoss.Weapon;

public class FireSuperDelayKnockup : FireDelayKnockup
{
	public static float shieldDuration;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			base.characterBody.AddTimedBuff(RoR2Content.Buffs.EngiShield, shieldDuration);
		}
	}
}
