using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public static class RewiredIntegrationManager
{
	public static List<Controller> blockedControllers = new List<Controller>();

	public static List<ulong> usersToReconnect = new List<ulong>();

	public static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Rewired Input Manager");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			if (x.Result != null)
			{
				FinishInit(x.Result);
			}
		};
	}

	public static void FinishInit(GameObject result)
	{
		Object.Instantiate(result);
		ReInput.ControllerConnectedEvent += AssignNewController;
		ReInput.ControllerPreDisconnectEvent += UnassignController;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
		ControllerType[] array = new ControllerType[3]
		{
			ControllerType.Keyboard,
			ControllerType.Mouse,
			ControllerType.Joystick
		};
		foreach (ControllerType controllerType in array)
		{
			Controller[] controllers = ReInput.controllers.GetControllers(controllerType);
			if (controllers != null)
			{
				for (int j = 0; j < controllers.Length; j++)
				{
					AssignNewController(controllers[j]);
				}
			}
		}
		LocalUser.LoadInitialUIMap();
	}

	private static void AssignJoystickToAvailablePlayer(Controller controller)
	{
		IList<Player> players = ReInput.players.Players;
		for (int i = 0; i < players.Count; i++)
		{
			Player player = players[i];
			if (player.name != "PlayerMain" && ((player.controllers.joystickCount == 0 && !player.controllers.hasKeyboard && !player.controllers.hasMouse) || ReInput.controllers.IsControllerAssignedToPlayer(controller.type, controller.id, player.id)))
			{
				player.controllers.AddController(controller, removeFromOtherPlayers: true);
				UserManager.NotifyControllerAssigned(controller.id, player);
				break;
			}
		}
	}

	private static void AssignNewController(ControllerStatusChangedEventArgs args)
	{
		AssignNewController(ReInput.controllers.GetController(args.controllerType, args.controllerId));
	}

	private static void AssignNewController(Controller controller)
	{
		if (!ReInput.controllers.IsControllerAssigned(controller.type, controller))
		{
			ReInput.players.GetPlayer("PlayerMain").controllers.AddController(controller, removeFromOtherPlayers: true);
			UserManager.NotifyControllerAssigned(controller.id, ReInput.players.GetPlayer("PlayerMain"));
		}
	}

	private static void UnassignController(ControllerStatusChangedEventArgs args)
	{
		Controller controller = ReInput.controllers.GetController(args.controllerType, args.controllerId);
		foreach (Player allPlayer in ReInput.players.AllPlayers)
		{
			if (ReInput.controllers.IsControllerAssignedToPlayer(args.controllerType, args.controllerId, allPlayer.id))
			{
				UserManager.NotifyControllerUnassigned(controller.id, allPlayer);
				break;
			}
		}
		ReInput.controllers.RemoveControllerFromAllPlayers(controller);
		if (blockedControllers.Contains(controller))
		{
			blockedControllers.Remove(controller);
		}
	}

	private static void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		RefreshJoystickAssignment();
	}

	public static void RefreshJoystickAssignment()
	{
		Controller[] controllers = ReInput.controllers.GetControllers(ControllerType.Joystick);
		if (controllers == null)
		{
			return;
		}
		Controller[] array = controllers;
		foreach (Controller controller in array)
		{
			if (!blockedControllers.Contains(controller) && !ReInput.controllers.IsControllerAssigned(ControllerType.Joystick, controller))
			{
				AssignJoystickToAvailablePlayer(controller);
			}
		}
	}

	public static void RefreshAfterRunFinish()
	{
		usersToReconnect.Clear();
		blockedControllers.Clear();
		RefreshJoystickAssignment();
	}

	public static Player GetPlayerUsingJoystickId(int slot)
	{
		foreach (Player player in ReInput.players.Players)
		{
			if (ReInput.controllers.IsJoystickAssignedToPlayer(slot, player.id))
			{
				return player;
			}
		}
		return null;
	}
}
