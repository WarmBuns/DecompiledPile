using UnityEngine.Networking;

namespace EntityStates.QuestVolatileBattery;

public class Monitor : QuestVolatileBatteryBaseState
{
	private float previousHealthFraction;

	private static readonly float healthFractionDetonationThreshold = 0.5f;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	private void FixedUpdateServer()
	{
		if ((bool)base.attachedHealthComponent)
		{
			float combinedHealthFraction = base.attachedHealthComponent.combinedHealthFraction;
			if (combinedHealthFraction <= healthFractionDetonationThreshold && healthFractionDetonationThreshold < previousHealthFraction)
			{
				outer.SetNextState(new CountDown());
			}
			previousHealthFraction = combinedHealthFraction;
		}
	}
}
