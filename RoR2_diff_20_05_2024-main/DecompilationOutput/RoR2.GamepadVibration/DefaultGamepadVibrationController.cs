using HG;

namespace RoR2.GamepadVibration;

public class DefaultGamepadVibrationController : GamepadVibrationController
{
	protected override void CalculateMotorValues(in VibrationContext vibrationContext, float[] motorValues)
	{
		ArrayUtils.SetRange(motorValues, vibrationContext.CalcCamDisplacementMagnitude(), 0, motorValues.Length);
	}
}
