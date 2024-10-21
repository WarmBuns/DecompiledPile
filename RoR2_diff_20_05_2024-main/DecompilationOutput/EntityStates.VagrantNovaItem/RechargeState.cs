using UnityEngine;

namespace EntityStates.VagrantNovaItem;

public class RechargeState : BaseVagrantNovaItemState
{
	public static float baseDuration = 30f;

	public static AnimationCurve particleEmissionCurve;

	private float rechargeDuration;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			int itemStack = GetItemStack();
			float num = baseDuration / (float)(itemStack + 1);
			float num2 = base.fixedAge / num;
			if (num2 >= 1f)
			{
				num2 = 1f;
				outer.SetNextState(new ReadyState());
			}
			SetChargeSparkEmissionRateMultiplier(particleEmissionCurve.Evaluate(num2));
		}
	}
}
