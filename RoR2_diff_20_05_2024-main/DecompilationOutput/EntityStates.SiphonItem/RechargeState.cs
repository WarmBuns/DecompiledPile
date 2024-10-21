using UnityEngine;

namespace EntityStates.SiphonItem;

public class RechargeState : BaseSiphonItemState
{
	public static float baseDuration = 30f;

	public static AnimationCurve particleEmissionCurve;

	private float rechargeDuration;

	public override void OnEnter()
	{
		base.OnEnter();
		TurnOffHealingFX();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			int itemStack = GetItemStack();
			float num = baseDuration / (float)(itemStack + 1);
			if (base.fixedAge / num >= 1f)
			{
				outer.SetNextState(new ReadyState());
			}
		}
	}
}
