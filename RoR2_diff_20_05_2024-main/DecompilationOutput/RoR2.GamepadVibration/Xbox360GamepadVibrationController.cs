using Rewired;

namespace RoR2.GamepadVibration;

public class Xbox360GamepadVibrationController : GamepadVibrationController
{
	protected static readonly float deepRumbleFactor = 5f;

	protected override void CalculateMotorValues(in VibrationContext vibrationContext, float[] motorValues)
	{
		float num = vibrationContext.CalcCamDisplacementMagnitude();
		float num2 = num / deepRumbleFactor;
		float num3 = num;
		motorValues[0] = num2;
		motorValues[1] = num3;
	}

	[GamepadVibrationControllerResolver(typeof(Xbox360GamepadVibrationController))]
	private static bool Resolve(Joystick joystick)
	{
		if (joystick.vibrationMotorCount >= 2)
		{
			if (!joystick.name.Contains("Xbox"))
			{
				return joystick.name.Contains("XInput Gamepad ");
			}
			return true;
		}
		return false;
	}
}
