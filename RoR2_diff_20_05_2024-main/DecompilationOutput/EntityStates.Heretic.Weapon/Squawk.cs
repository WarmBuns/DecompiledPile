using RoR2;
using UnityEngine;

namespace EntityStates.Heretic.Weapon;

public class Squawk : EntityState
{
	[SerializeField]
	public string soundName;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(soundName, base.gameObject);
		outer.SetNextStateToMain();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
