using UnityEngine;

namespace EntityStates;

public class BaseMelee : BaseState
{
	protected RootMotionAccumulator rootMotionAccumulator;

	public RootMotionAccumulator InitMeleeRootMotion()
	{
		rootMotionAccumulator = GetModelRootMotionAccumulator();
		if ((bool)rootMotionAccumulator)
		{
			rootMotionAccumulator.ExtractRootMotion();
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.forward = base.inputBank.aimDirection;
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.moveDirection = Vector3.zero;
		}
		return rootMotionAccumulator;
	}

	public void UpdateMeleeRootMotion(float scale)
	{
		if ((bool)rootMotionAccumulator)
		{
			Vector3 vector = rootMotionAccumulator.ExtractRootMotion();
			if ((bool)base.characterMotor)
			{
				base.characterMotor.rootMotion = vector * scale;
			}
		}
	}
}
