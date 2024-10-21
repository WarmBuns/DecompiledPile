using System;
using System.Text;
using Rewired;
using RoR2.Stats;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

public class LocalUser
{
	private Player _inputPlayer;

	private UserProfile _userProfile;

	public int id;

	public PauseScreenController pauseScreenController;

	private CameraRigController _cameraRigController;

	public Player inputPlayer
	{
		get
		{
			return _inputPlayer;
		}
		set
		{
			if (_inputPlayer != value)
			{
				if (_inputPlayer != null)
				{
					OnRewiredPlayerLost(_inputPlayer);
				}
				_inputPlayer = value;
				eventSystem = MPEventSystemManager.FindEventSystem(_inputPlayer);
				if (_inputPlayer != null)
				{
					OnRewiredPlayerDiscovered(_inputPlayer);
				}
			}
		}
	}

	public MPEventSystem eventSystem { get; private set; }

	public UserProfile userProfile
	{
		get
		{
			return _userProfile;
		}
		set
		{
			_userProfile = value;
			ApplyUserProfileBindingsToRewiredPlayer();
		}
	}

	public bool isUIFocused
	{
		get
		{
			if (!eventSystem.currentSelectedGameObject && !eventSystem.IsPointerOverGameObject())
			{
				return pauseScreenController != null;
			}
			return true;
		}
	}

	public NetworkUser currentNetworkUser { get; private set; }

	public PlayerCharacterMasterController cachedMasterController { get; private set; }

	public CharacterMaster cachedMaster { get; private set; }

	public GameObject cachedMasterObject { get; private set; }

	public CharacterBody cachedBody { get; private set; }

	public GameObject cachedBodyObject { get; private set; }

	public PlayerStatsComponent cachedStatsComponent { get; private set; }

	public CameraRigController cameraRigController
	{
		get
		{
			return _cameraRigController;
		}
		set
		{
			if ((object)_cameraRigController != value)
			{
				if ((object)_cameraRigController != null)
				{
					this.onCameraLost?.Invoke(_cameraRigController);
				}
				_cameraRigController = value;
				if ((object)_cameraRigController != null)
				{
					this.onCameraDiscovered?.Invoke(_cameraRigController);
				}
			}
		}
	}

	public event Action onBodyChanged;

	public event Action onMasterChanged;

	public event Action<CameraRigController> onCameraDiscovered;

	public event Action<CameraRigController> onCameraLost;

	public event Action<NetworkUser> onNetworkUserFound;

	public event Action<NetworkUser> onNetworkUserLost;

	static LocalUser()
	{
		ReInput.ControllerConnectedEvent += OnControllerConnected;
		ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
	}

	private static void OnControllerConnected(ControllerStatusChangedEventArgs args)
	{
		DefaultControllerMaps.RewiredDebugLog($"OnControllerConnected: Name {args.name}, Type {args.controllerType}, Id {args.controllerId}");
		int count = LocalUserManager.readOnlyLocalUsersList.Count;
		for (int i = 0; i < count; i++)
		{
			LocalUser localUser = LocalUserManager.readOnlyLocalUsersList[i];
			if (localUser.inputPlayer.controllers.ContainsController(args.controllerType, args.controllerId))
			{
				localUser.OnControllerDiscovered(ReInput.controllers.GetController(args.controllerType, args.controllerId));
			}
		}
	}

	private static void OnControllerDisconnected(ControllerStatusChangedEventArgs args)
	{
		DefaultControllerMaps.RewiredDebugLog($"OnControllerDisconnected: Name {args.name}, Type {args.controllerType}, Id {args.controllerId}");
		int count = LocalUserManager.readOnlyLocalUsersList.Count;
		for (int i = 0; i < count; i++)
		{
			LocalUser localUser = LocalUserManager.readOnlyLocalUsersList[i];
			if (localUser.inputPlayer.controllers.ContainsController(args.controllerType, args.controllerId))
			{
				if (args.controllerType == ControllerType.Joystick && ReInput.controllers.GetControllers(ControllerType.Joystick).Length == 1)
				{
					break;
				}
				localUser.OnControllerLost(ReInput.controllers.GetController(args.controllerType, args.controllerId));
			}
		}
	}

	public static void DumpControllerMap(ControllerMap inControllerMap, StringBuilder inDumpSB = null, bool inForceDump = false)
	{
		if ((!DefaultControllerMaps.RewiredLoggingEnabled() && !inForceDump) || inControllerMap == null)
		{
			return;
		}
		if (inDumpSB == null)
		{
			new StringBuilder();
		}
		foreach (ActionElementMap allMap in inControllerMap.AllMaps)
		{
			_ = allMap;
		}
	}

	private void OnRewiredPlayerDiscovered(Player player)
	{
		foreach (Controller controller in player.controllers.Controllers)
		{
			DefaultControllerMaps.RewiredDebugLog("- HERE -------------------------------------------------------------------------");
			DefaultControllerMaps.RewiredDebugLog($"Dumping maps for controller {controller.hardwareName} (Ident {controller.hardwareIdentifier}, HWGUID {controller.hardwareTypeGuid})");
			DumpControllerMap(player.controllers.maps.GetMap(controller, "Default", "Default"));
			DumpControllerMap(player.controllers.maps.GetMap(controller, "UI", "UI"));
			OnControllerDiscovered(controller);
		}
	}

	private void OnRewiredPlayerLost(Player player)
	{
		foreach (Controller controller in player.controllers.Controllers)
		{
			OnControllerLost(controller);
		}
	}

	private void OnControllerDiscovered(Controller controller)
	{
		ApplyUserProfileBindingsToRewiredPlayer();
	}

	private void OnControllerLost(Controller controller)
	{
		inputPlayer.controllers.maps.ClearMapsForController(controller.type, controller.id, userAssignableOnly: true);
	}

	private static bool UseJpLayout()
	{
		return false;
	}

	public static void LoadInitialUIMap()
	{
		bool flag = UseJpLayout();
		foreach (Player player in ReInput.players.Players)
		{
			player.controllers.maps.ClearAllMaps(userAssignableOnly: false);
			foreach (Controller controller in player.controllers.Controllers)
			{
				if (controller.type == ControllerType.Joystick && flag)
				{
					player.controllers.maps.LoadMap(controller.type, controller.id, 2, 1);
					if (InputBindingDisplayController.onBindingsChanged != null)
					{
						InputBindingDisplayController.onBindingsChanged();
					}
					continue;
				}
				try
				{
					player.controllers.maps.LoadMap(controller.type, controller.id, 2, 0);
				}
				catch (FormatException exception)
				{
					Debug.LogWarning($"Excepting loading controller mapping (type:{controller.type},id:{controller.id}) for player (name:{player.name},id:{player.id}).");
					Debug.LogException(exception);
				}
			}
			player.controllers.maps.SetAllMapsEnabled(state: true);
		}
	}

	public void ApplyUserProfileBindingsToRewiredPlayer(bool requirePlayer = true)
	{
		if (inputPlayer == null || userProfile == null)
		{
			return;
		}
		bool flag = UseJpLayout();
		foreach (Player player in ReInput.players.Players)
		{
			foreach (Controller controller in player.controllers.Controllers)
			{
				DefaultControllerMaps.RewiredDebugLog("***** ApplyUserProfileBindingsToRewiredPlayer> Player {0}, Controller {1} (Type {2}, HWName {3}, HWGUID {4})", (player != null) ? player.name : "*null*", (controller != null) ? controller.name : "*null*", (controller != null) ? controller.type.ToString() : "*null*", (controller != null) ? controller.hardwareName : "*null*", (controller != null) ? controller.hardwareTypeGuid.ToString() : "-----");
				if (controller.type == ControllerType.Joystick && flag)
				{
					player.controllers.maps.RemoveMap(controller.type, controller.id, 2, 0);
					player.controllers.maps.LoadMap(controller.type, controller.id, 2, 1);
					if (InputBindingDisplayController.onBindingsChanged != null)
					{
						InputBindingDisplayController.onBindingsChanged();
					}
				}
				else if (controller.type != ControllerType.Joystick)
				{
					try
					{
						player.controllers.maps.LoadMap(controller.type, controller.id, 2, 0);
					}
					catch (FormatException exception)
					{
						Debug.LogWarning($"Excepting loading controller mapping (type:{controller.type},id:{controller.id}) for player (name:{player.name},id:{player.id}).");
						Debug.LogException(exception);
					}
				}
				ApplyUserProfileBindingstoRewiredController(player, controller);
			}
			player.controllers.maps.SetAllMapsEnabled(state: true);
		}
		RoR2Application.DebugPrintRewired();
		void ApplyUserProfileBindingstoRewiredController(Player iterPlayer, Controller controller)
		{
			if (userProfile != null)
			{
				ControllerMap controllerMap = null;
				switch (controller.type)
				{
				case ControllerType.Keyboard:
					controllerMap = userProfile.keyboardMap;
					break;
				case ControllerType.Mouse:
					controllerMap = userProfile.mouseMap;
					break;
				case ControllerType.Joystick:
					controllerMap = userProfile.GetJoystickMap(controller.hardwareTypeGuid);
					break;
				}
				if (controllerMap != null)
				{
					DefaultControllerMaps.RewiredDebugLog("***** ApplyUserProfileBindingstoRewiredController> Player {0}, Controller {1} (Type {2}, HWName {3}, HWGUID {4})", (iterPlayer != null) ? iterPlayer.name : "*null*", (controller != null) ? controller.name : "*null*", (controller != null) ? controller.type.ToString() : "*null*", (controller != null) ? controller.hardwareName : "*null*", (controller != null) ? controller.hardwareTypeGuid.ToString() : "-----");
					DumpControllerMap(controllerMap);
					inputPlayer.controllers.maps.AddMap(controller, controllerMap);
				}
			}
		}
	}

	public UIInputPassthrough GetUIPassthrough()
	{
		if ((bool)eventSystem && (bool)eventSystem.currentSelectedGameObject)
		{
			return eventSystem.currentSelectedGameObject.GetComponent<UIInputPassthrough>();
		}
		return null;
	}

	public void RebuildControlChain()
	{
		PlayerCharacterMasterController playerCharacterMasterController = cachedMasterController;
		cachedMasterController = null;
		cachedMasterObject = null;
		cachedMaster = null;
		cachedStatsComponent = null;
		CharacterBody characterBody = cachedBody;
		cachedBody = null;
		cachedBodyObject = null;
		if ((bool)currentNetworkUser)
		{
			cachedMasterObject = currentNetworkUser.masterObject;
			if ((bool)cachedMasterObject)
			{
				cachedMasterController = cachedMasterObject.GetComponent<PlayerCharacterMasterController>();
			}
			if ((bool)cachedMasterController)
			{
				cachedMaster = cachedMasterController.master;
				if ((bool)cachedMaster)
				{
					cachedStatsComponent = cachedMaster.playerStatsComponent;
				}
				cachedBody = cachedMaster.GetBody();
				if ((bool)cachedBody)
				{
					cachedBodyObject = cachedBody.gameObject;
				}
			}
		}
		if ((object)characterBody != cachedBody)
		{
			this.onBodyChanged?.Invoke();
		}
		if ((object)playerCharacterMasterController != cachedMasterController)
		{
			this.onMasterChanged?.Invoke();
		}
	}

	public void LinkNetworkUser(NetworkUser newNetworkUser)
	{
		if (!currentNetworkUser)
		{
			currentNetworkUser = newNetworkUser;
			newNetworkUser.localUser = this;
			this.onNetworkUserFound?.Invoke(newNetworkUser);
		}
	}

	public void UnlinkNetworkUser()
	{
		this.onNetworkUserLost?.Invoke(currentNetworkUser);
		currentNetworkUser.localUser = null;
		currentNetworkUser = null;
		cachedMasterController = null;
		cachedMasterObject = null;
		cachedBody = null;
		cachedBodyObject = null;
	}
}
