using System;
using HG.Reflection;

namespace RoR2.GamepadVibration;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class GamepadVibrationControllerResolverAttribute : HG.Reflection.SearchableAttribute
{
	public readonly Type vibrationControllerType;

	public GamepadVibrationControllerResolverAttribute(Type vibrationControllerType)
	{
		this.vibrationControllerType = vibrationControllerType;
	}
}
