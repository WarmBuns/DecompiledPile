using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HG;
using HG.Collections.Generic;
using Rewired;
using RoR2.UI;
using UnityEngine;

namespace RoR2.GamepadVibration;

public static class GamepadVibrationManager
{
	private static readonly AssociationList<Joystick, GamepadVibrationController> joystickToVibrationController = new AssociationList<Joystick, GamepadVibrationController>(4);

	private static int[] joystickIndexToLocalUserIndex = new int[4];

	[SystemInitializer(new Type[] { typeof(GamepadVibrationController) })]
	private static void Init()
	{
		RoR2Application.onUpdate += Update;
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		ReInput.ControllerPreDisconnectEvent += OnControllerPreDisconnect;
		RoR2Application.onShutDown = (Action)Delegate.Combine(RoR2Application.onShutDown, new Action(OnApplicationShutDown));
		foreach (Controller controller in ReInput.controllers.Controllers)
		{
			OnControllerConnected(new ControllerStatusChangedEventArgs(controller.name, controller.id, controller.type));
		}
	}

	private static void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		if (args.controller is Joystick joystick)
		{
			joystickToVibrationController.Add(joystick, GamepadVibrationController.Create(joystick));
		}
	}

	private static void OnControllerPreDisconnect(ControllerStatusChangedEventArgs args)
	{
		if (args.controller is Joystick key)
		{
			int num = joystickToVibrationController.FindKeyIndex(key);
			if (num >= 0)
			{
				joystickToVibrationController[num].Value.Dispose();
				joystickToVibrationController.RemoveAt(num);
			}
		}
	}

	private static void OnApplicationShutDown()
	{
		for (int num = joystickToVibrationController.Count - 1; num >= 0; num--)
		{
			joystickToVibrationController[num].Value.Dispose();
			joystickToVibrationController.RemoveAt(num);
		}
	}

	private static void Update()
	{
		if (Time.deltaTime <= 0f)
		{
			foreach (KeyValuePair<Joystick, GamepadVibrationController> item in joystickToVibrationController)
			{
				item.Value.StopVibration();
			}
			return;
		}
		ReadOnlyCollection<LocalUser> readOnlyLocalUsersList = LocalUserManager.readOnlyLocalUsersList;
		ArrayUtils.EnsureCapacity(ref joystickIndexToLocalUserIndex, joystickToVibrationController.Count);
		ArrayUtils.SetRange(joystickIndexToLocalUserIndex, -1, 0, joystickToVibrationController.Count);
		for (int i = 0; i < readOnlyLocalUsersList.Count; i++)
		{
			LocalUser localUser = readOnlyLocalUsersList[i];
			if (localUser.inputPlayer != null && (bool)localUser.eventSystem && localUser.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad && localUser.inputPlayer?.controllers.GetLastActiveController<Joystick>() is Joystick key)
			{
				int num = joystickToVibrationController.FindKeyIndex(key);
				if (num >= 0)
				{
					joystickIndexToLocalUserIndex[num] = i;
				}
			}
		}
		for (int j = 0; j < joystickToVibrationController.Count; j++)
		{
			GamepadVibrationController value = joystickToVibrationController[j].Value;
			int num2 = joystickIndexToLocalUserIndex[j];
			LocalUser localUser2 = ((num2 >= 0) ? readOnlyLocalUsersList[num2] : null);
			if (localUser2 != null && localUser2.userProfile != null)
			{
				VibrationContext vibrationContext = default(VibrationContext);
				vibrationContext.localUser = localUser2;
				vibrationContext.cameraRigController = localUser2.cameraRigController;
				vibrationContext.userVibrationScale = localUser2.userProfile.gamepadVibrationScale;
				VibrationContext vibrationContext2 = vibrationContext;
				value.ApplyVibration(in vibrationContext2);
			}
			else
			{
				value.StopVibration();
			}
		}
	}
}
