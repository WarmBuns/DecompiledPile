using System;
using System.Collections.Generic;
using System.Reflection;
using HG.Reflection;
using Rewired;
using UnityEngine;

namespace RoR2.GamepadVibration;

public abstract class GamepadVibrationController : IDisposable
{
	private Joystick _joystick;

	private float[] motorValues = Array.Empty<float>();

	private static List<(Func<Joystick, bool> predicate, Type vibrationControllerType)> vibrationControllerTypeResolver = new List<(Func<Joystick, bool>, Type)>();

	private static Type defaultVibrationControllerType = typeof(DefaultGamepadVibrationController);

	public Joystick joystick
	{
		get
		{
			return _joystick;
		}
		private set
		{
			if (_joystick != value)
			{
				_joystick = value;
				if (_joystick != null)
				{
					Array.Resize(ref motorValues, _joystick.vibrationMotorCount);
					OnJoystickAssigned(_joystick);
				}
			}
		}
	}

	protected virtual void OnJoystickAssigned(Joystick joystick)
	{
	}

	public void Dispose()
	{
		StopVibration();
		DisposeInternal();
		joystick = null;
	}

	protected virtual void DisposeInternal()
	{
	}

	public void ApplyVibration(in VibrationContext vibrationContext)
	{
		Array.Clear(motorValues, 0, motorValues.Length);
		try
		{
			CalculateMotorValues(in vibrationContext, motorValues);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		float userVibrationScale = vibrationContext.userVibrationScale;
		for (int i = 0; i < motorValues.Length; i++)
		{
			joystick.SetVibration(i, motorValues[i] * userVibrationScale);
		}
		try
		{
			ApplyNonStandardVibration(in vibrationContext);
		}
		catch (Exception message2)
		{
			Debug.LogError(message2);
		}
	}

	protected virtual void CalculateMotorValues(in VibrationContext vibrationContext, float[] motorValues)
	{
	}

	protected virtual void ApplyNonStandardVibration(in VibrationContext vibrationContext)
	{
	}

	public void StopVibration()
	{
		try
		{
			StopNonStandardVibration();
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		joystick.StopVibration();
	}

	protected virtual void StopNonStandardVibration()
	{
	}

	public static GamepadVibrationController Create(Joystick joystick)
	{
		Type item = defaultVibrationControllerType;
		for (int i = 0; i < vibrationControllerTypeResolver.Count; i++)
		{
			try
			{
				(Func<Joystick, bool>, Type) tuple = vibrationControllerTypeResolver[i];
				if (tuple.Item1(joystick))
				{
					item = tuple.Item2;
					break;
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		GamepadVibrationController obj = (GamepadVibrationController)Activator.CreateInstance(item);
		obj.joystick = joystick;
		return obj;
	}

	public static void RegisterResolver(Func<Joystick, bool> predicate, Type vibrationControllerType)
	{
		predicate = predicate ?? throw new ArgumentNullException("predicate");
		vibrationControllerType = vibrationControllerType ?? throw new ArgumentNullException("vibrationControllerType");
		if (!vibrationControllerType.IsSubclassOf(typeof(GamepadVibrationController)))
		{
			throw new ArgumentException("vibrationControllerType must inherit from GamepadVibrationController");
		}
		vibrationControllerTypeResolver.Add((predicate, vibrationControllerType));
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		List<GamepadVibrationControllerResolverAttribute> list = new List<GamepadVibrationControllerResolverAttribute>();
		HG.Reflection.SearchableAttribute.GetInstances(list);
		foreach (GamepadVibrationControllerResolverAttribute item in list)
		{
			try
			{
				MethodInfo methodInfo = (MethodInfo)item.target;
				if (!methodInfo.IsStatic)
				{
					throw new Exception("GamepadVibrationControllerResolverAttribute target must be a static method. target=" + methodInfo.DeclaringType.FullName + "." + methodInfo.Name);
				}
				RegisterResolver((Func<Joystick, bool>)methodInfo.CreateDelegate(typeof(Func<Joystick, bool>)), item.vibrationControllerType);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
	}
}
