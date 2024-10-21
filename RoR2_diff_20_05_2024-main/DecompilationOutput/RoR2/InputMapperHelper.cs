using System;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

public class InputMapperHelper : IDisposable
{
	private readonly MPEventSystem eventSystem;

	private readonly List<InputMapper> inputMappers = new List<InputMapper>();

	private ControllerMap[] maps = Array.Empty<ControllerMap>();

	private SimpleDialogBox dialogBox;

	public float timeout = 5f;

	private float timer;

	private Player currentPlayer;

	private InputAction currentAction;

	private AxisRange currentAxisRange;

	public static bool anyRemapperListening = false;

	public static float anyRemapperListeningStopTimestamp = -1f;

	protected Player _player;

	private Action<InputMapper.ConflictResponse> conflictResponseCallback;

	private static readonly HashSet<string> forbiddenElements = new HashSet<string>
	{
		"Left Stick X",
		"left stick x",
		"Left Stick Y",
		"left stick y",
		"Right Stick X",
		"right stick x",
		"Right Stick Y",
		"right stick y",
		"Mouse Horizontal",
		"Mouse Vertical",
		"Menu",
		"Menu Button",
		"OPTIONS button",
		"options button",
		"Left SL",
		"Left SR",
		"Right SL",
		"Right SR",
		"+ Button",
		Keyboard.GetKeyName(KeyCode.Escape),
		Keyboard.GetKeyName(KeyCode.KeypadEnter),
		Keyboard.GetKeyName(KeyCode.Return)
	};

	public bool isListening { get; private set; }

	public InputMapperHelper(MPEventSystem eventSystem)
	{
		this.eventSystem = eventSystem;
	}

	private InputMapper AddInputMapper()
	{
		InputMapper inputMapper = new InputMapper();
		inputMapper.ConflictFoundEvent += InputMapperOnConflictFoundEvent;
		inputMapper.CanceledEvent += InputMapperOnCanceledEvent;
		inputMapper.ErrorEvent += InputMapperOnErrorEvent;
		inputMapper.InputMappedEvent += InputMapperOnInputMappedEvent;
		inputMapper.StartedEvent += InputMapperOnStartedEvent;
		inputMapper.StoppedEvent += InputMapperOnStoppedEvent;
		inputMapper.TimedOutEvent += InputMapperOnTimedOutEvent;
		inputMapper.options = new InputMapper.Options
		{
			allowAxes = true,
			allowButtons = true,
			allowKeyboardKeysWithModifiers = false,
			allowKeyboardModifierKeyAsPrimary = true,
			checkForConflicts = true,
			checkForConflictsWithAllPlayers = false,
			checkForConflictsWithPlayerIds = Array.Empty<int>(),
			checkForConflictsWithSelf = true,
			checkForConflictsWithSystemPlayer = false,
			defaultActionWhenConflictFound = InputMapper.ConflictResponse.Add,
			holdDurationToMapKeyboardModifierKeyAsPrimary = 0f,
			ignoreMouseXAxis = true,
			ignoreMouseYAxis = true,
			timeout = float.PositiveInfinity
		};
		inputMappers.Add(inputMapper);
		return inputMapper;
	}

	private void RemoveInputMapper(InputMapper inputMapper)
	{
		inputMapper.ConflictFoundEvent -= InputMapperOnConflictFoundEvent;
		inputMapper.CanceledEvent -= InputMapperOnCanceledEvent;
		inputMapper.ErrorEvent -= InputMapperOnErrorEvent;
		inputMapper.InputMappedEvent -= InputMapperOnInputMappedEvent;
		inputMapper.StartedEvent -= InputMapperOnStartedEvent;
		inputMapper.StoppedEvent -= InputMapperOnStoppedEvent;
		inputMapper.TimedOutEvent -= InputMapperOnTimedOutEvent;
		inputMappers.Remove(inputMapper);
	}

	public void Start(Player player, IList<Controller> controllers, InputAction action, AxisRange axisRange)
	{
		Stop();
		anyRemapperListening = true;
		isListening = true;
		currentPlayer = player;
		currentAction = action;
		currentAxisRange = axisRange;
		maps = (from controller in controllers
			select player.controllers.maps.GetFirstMapInCategory(controller, 0) into map
			where map != null
			select map).ToArray();
		DefaultControllerMaps.RewiredDebugLog("InputMapperHelper: Found {0} control maps", maps.Length);
		_player = player;
		Controller lastActiveController = player.controllers.GetLastActiveController();
		Controller lastActiveController2 = player.controllers.GetLastActiveController(ControllerType.Joystick);
		DefaultControllerMaps.RewiredDebugLog("InputMapHelper.Start: active {0} ({1}), activeJoystick {2} ({3})", (lastActiveController != null) ? lastActiveController.type.ToString() : "**null**", (lastActiveController != null) ? lastActiveController.hardwareTypeGuid.ToString() : "**null**", (lastActiveController2 != null) ? lastActiveController2.type.ToString() : "**null**", (lastActiveController2 != null) ? lastActiveController2.hardwareTypeGuid.ToString() : "**null**");
		if (lastActiveController2 != null && lastActiveController == lastActiveController2)
		{
			ControllerMap[] array = maps;
			foreach (ControllerMap controllerMap in array)
			{
				DefaultControllerMaps.RewiredDebugLog("\tProcessing map: {0}", controllerMap.ToJsonString());
				InputMapper.Context mappingContext = new InputMapper.Context
				{
					actionId = action.id,
					controllerMap = controllerMap,
					actionRange = currentAxisRange
				};
				AddInputMapper().Start(mappingContext);
			}
		}
		else
		{
			ControllerMap[] array = maps;
			foreach (ControllerMap controllerMap2 in array)
			{
				if (controllerMap2.controllerType != ControllerType.Joystick)
				{
					InputMapper.Context mappingContext2 = new InputMapper.Context
					{
						actionId = action.id,
						controllerMap = controllerMap2,
						actionRange = currentAxisRange
					};
					AddInputMapper().Start(mappingContext2);
				}
			}
		}
		dialogBox = SimpleDialogBox.Create(eventSystem);
		timer = timeout;
		UpdateDialogBoxString();
		RoR2Application.onUpdate += Update;
	}

	public void Stop()
	{
		if (isListening)
		{
			anyRemapperListening = false;
			anyRemapperListeningStopTimestamp = Time.unscaledTime;
			maps = Array.Empty<ControllerMap>();
			currentPlayer = null;
			currentAction = null;
			for (int num = inputMappers.Count - 1; num >= 0; num--)
			{
				InputMapper inputMapper = inputMappers[num];
				inputMapper.Stop();
				RemoveInputMapper(inputMapper);
			}
			if ((bool)dialogBox)
			{
				UnityEngine.Object.Destroy(dialogBox.rootObject);
				dialogBox = null;
			}
			isListening = false;
			RoR2Application.onUpdate -= Update;
		}
	}

	private void Update()
	{
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		if (isListening)
		{
			timer -= unscaledDeltaTime;
			if (timer < 0f)
			{
				Stop();
			}
			else if (currentPlayer.GetButtonDown(25))
			{
				ShowCancledDialog();
			}
			else
			{
				UpdateDialogBoxString();
			}
		}
	}

	private void ShowCancledDialog()
	{
		Stop();
		SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create(eventSystem);
		simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("OPTION_REBIND_DIALOG_TITLE");
		simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("OPTION_REBIND_CANCELLED_DIALOG_DESCRIPTION");
		simpleDialogBox.AddCancelButton(CommonLanguageTokens.ok);
	}

	private void UpdateDialogBoxString()
	{
		if ((bool)dialogBox && timer >= 0f)
		{
			string @string = Language.GetString(InputCatalog.GetActionNameToken(currentAction.name));
			dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
			{
				token = CommonLanguageTokens.optionRebindDialogTitle,
				formatParams = Array.Empty<object>()
			};
			dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
			{
				token = CommonLanguageTokens.optionRebindDialogDescription,
				formatParams = new object[2] { @string, timer }
			};
		}
	}

	private void InputMapperOnTimedOutEvent(InputMapper.TimedOutEventData timedOutEventData)
	{
	}

	private void InputMapperOnStoppedEvent(InputMapper.StoppedEventData stoppedEventData)
	{
	}

	private void InputMapperOnStartedEvent(InputMapper.StartedEventData startedEventData)
	{
	}

	private void InputMapperOnInputMappedEvent(InputMapper.InputMappedEventData inputMappedEventData)
	{
		ActionElementMap incomingActionElementMap = inputMappedEventData.actionElementMap;
		int incomingActionId = inputMappedEventData.actionElementMap.actionId;
		int incomingElementIndex = inputMappedEventData.actionElementMap.elementIndex;
		ControllerElementType incomingElementType = inputMappedEventData.actionElementMap.elementType;
		ControllerMap map = inputMappedEventData.actionElementMap.controllerMap;
		Controller activeController = null;
		ControllerMap defaultMap = null;
		ControllerMap uiMap = null;
		if (_player != null)
		{
			activeController = _player.controllers.GetLastActiveController();
			if (activeController != null && activeController.type == ControllerType.Joystick)
			{
				if (!DefaultControllerMaps.HardwareGuidsMatch(activeController.hardwareTypeGuid, map.hardwareGuid))
				{
					DefaultControllerMaps.RewiredDebugLog($"Skipping hardware {map.hardwareGuid} (activeController {activeController.hardwareTypeGuid}");
					return;
				}
				DefaultControllerMaps.RewiredDebugLog($"Hardware match: {map.hardwareGuid} (activeController {activeController.hardwareTypeGuid}");
			}
		}
		if (activeController != null && activeController.type == ControllerType.Joystick)
		{
			defaultMap = ReInput.mapping.GetJoystickMapInstance(activeController.hardwareTypeGuid, 0, 0);
			uiMap = ReInput.mapping.GetJoystickMapInstance(activeController.hardwareTypeGuid, 2, 0);
		}
		DefaultControllerMaps.RewiredDebugLog($"NEWMAP: elementIdentifierID: {inputMappedEventData.actionElementMap.elementIdentifierId} ({inputMappedEventData.actionElementMap.actionDescriptiveName})");
		if (forbiddenElements.Contains(inputMappedEventData.actionElementMap.elementIdentifierName))
		{
			ShowCancledDialog();
			return;
		}
		ControllerMap[] array = maps;
		foreach (ControllerMap controllerMap in array)
		{
			if (controllerMap != map)
			{
				controllerMap.DeleteElementMapsWithAction(incomingActionId);
			}
		}
		while (DeleteFirstConflictingElementMap())
		{
		}
		if (activeController != null)
		{
			if (map is JoystickMap inJoystickMap)
			{
				eventSystem?.localUser?.userProfile.UpdateJoystickMap(activeController.hardwareTypeGuid, inJoystickMap);
			}
			else
			{
				DefaultControllerMaps.RewiredDebugWarn($"NOT A JOYSTICK MAP for controller {activeController.hardwareTypeGuid}");
			}
		}
		eventSystem?.localUser?.userProfile.RequestEventualSave();
		Stop();
		if (InputBindingDisplayController.onBindingsChanged != null)
		{
			InputBindingDisplayController.onBindingsChanged();
		}
		bool ActionElementMapConflicts(ActionElementMap actionElementMap)
		{
			if (actionElementMap == incomingActionElementMap)
			{
				return false;
			}
			bool num2 = actionElementMap.elementIndex == incomingElementIndex && actionElementMap.elementType == incomingElementType;
			bool flag2 = actionElementMap.actionId == incomingActionId && actionElementMap.axisContribution == incomingActionElementMap.axisContribution;
			return num2 || flag2;
		}
		bool DeleteFirstConflictingElementMap()
		{
			foreach (ActionElementMap allMap in map.AllMaps)
			{
				if ((defaultMap != null || uiMap != null) && allMap.controllerMap != null)
				{
					bool num = allMap.controllerMap == defaultMap;
					bool flag = allMap.controllerMap != uiMap;
					if (!num && !flag)
					{
						DefaultControllerMaps.RewiredDebugLog("Skipping map as it is for wrong controller... active {0} vs checking {1}", activeController.hardwareTypeGuid, allMap.controllerMap.controller.hardwareTypeGuid);
						continue;
					}
				}
				if (ActionElementMapConflicts(allMap))
				{
					Debug.LogFormat("Deleting conflicting mapping {0}", allMap);
					DefaultControllerMaps.RewiredDebugLog("***** Deleting conflicting mapping {0} ({1})", allMap, allMap.controllerMap.controller.hardwareTypeGuid);
					map.DeleteElementMap(allMap.id);
					return true;
				}
			}
			return false;
		}
	}

	private void InputMapperOnErrorEvent(InputMapper.ErrorEventData errorEventData)
	{
	}

	private void InputMapperOnCanceledEvent(InputMapper.CanceledEventData canceledEventData)
	{
	}

	private void InputMapperOnConflictFoundEvent(InputMapper.ConflictFoundEventData conflictFoundEventData)
	{
		InputMapper.ConflictResponse obj = ((!conflictFoundEventData.conflicts.Any((ElementAssignmentConflictInfo elementAssignmentConflictInfo) => forbiddenElements.Contains(elementAssignmentConflictInfo.elementIdentifier.name))) ? InputMapper.ConflictResponse.Add : InputMapper.ConflictResponse.Ignore);
		conflictFoundEventData.responseCallback(obj);
	}

	public void Dispose()
	{
		Stop();
	}
}
