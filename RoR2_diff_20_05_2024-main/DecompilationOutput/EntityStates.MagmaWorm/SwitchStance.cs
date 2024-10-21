using RoR2;
using UnityEngine.Networking;

namespace EntityStates.MagmaWorm;

public class SwitchStance : BaseState
{
	public static float leapingDuration = 10f;

	public static float groundStanceSpring = 3f;

	public static float groundStanceDamping = 3f;

	public static float groundStanceSpeedMultiplier = 1.5f;

	public static float leapStanceSpring = 3f;

	public static float leapStanceDamping = 1f;

	public static float leapStanceSpeedMultiplier = 1f;

	public override void OnEnter()
	{
		base.OnEnter();
		SetStanceParameters(leaping: true);
	}

	public override void OnExit()
	{
		base.OnExit();
		SetStanceParameters(leaping: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= leapingDuration)
		{
			outer.SetNextStateToMain();
		}
	}

	private void SetStanceParameters(bool leaping)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		WormBodyPositions2 component = GetComponent<WormBodyPositions2>();
		WormBodyPositionsDriver component2 = GetComponent<WormBodyPositionsDriver>();
		if ((bool)component)
		{
			if (leaping)
			{
				component2.ySpringConstant = leapStanceSpring;
				component2.yDamperConstant = leapStanceDamping;
				component.speedMultiplier = leapStanceSpeedMultiplier;
				component2.allowShoving = false;
			}
			else
			{
				component2.ySpringConstant = groundStanceSpring;
				component2.yDamperConstant = groundStanceDamping;
				component.speedMultiplier = groundStanceSpeedMultiplier;
				component2.allowShoving = true;
			}
			component.shouldFireMeatballsOnImpact = leaping;
		}
	}
}
