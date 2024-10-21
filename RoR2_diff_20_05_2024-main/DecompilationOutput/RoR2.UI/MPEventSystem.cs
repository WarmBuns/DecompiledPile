using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Rewired;
using Rewired.Integration.UnityUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RewiredStandaloneInputModule))]
public class MPEventSystem : EventSystem
{
	public enum InputSource
	{
		MouseAndKeyboard,
		Gamepad
	}

	private struct PushInfo
	{
		public int index;

		public Vector2 position;

		public Vector2 pushVector;
	}

	private static readonly List<MPEventSystem> instancesList;

	public static ReadOnlyCollection<MPEventSystem> readOnlyInstancesList;

	public int playerSlot = -1;

	[NonSerialized]
	public bool allowCursorPush = true;

	[NonSerialized]
	public bool isCombinedEventSystem;

	[HideInInspector]
	public MPButton currentSelectedButton;

	private CursorIndicatorController cursorIndicatorController;

	[NotNull]
	public Player player;

	[CanBeNull]
	public LocalUser localUser;

	public TooltipProvider currentTooltipProvider;

	public TooltipController currentTooltip;

	private static PushInfo[] pushInfos;

	private const float radius = 24f;

	private const float invRadius = 1f / 24f;

	private const float radiusSqr = 576f;

	private const float pushFactor = 10f;

	public static int activeCount { get; private set; }

	public int cursorOpenerCount { get; set; }

	public int cursorOpenerForGamepadCount { get; set; }

	public bool isHovering
	{
		get
		{
			if ((bool)base.currentInputModule)
			{
				return ((MPInputModule)base.currentInputModule).isHovering;
			}
			return false;
		}
	}

	public bool isCursorVisible => cursorIndicatorController.gameObject.activeInHierarchy;

	public InputSource currentInputSource { get; private set; } = InputSource.Gamepad;

	public InputMapperHelper inputMapperHelper { get; private set; }

	public static MPEventSystem FindByPlayer(Player player)
	{
		foreach (MPEventSystem instances in instancesList)
		{
			if (instances.player == player)
			{
				return instances;
			}
		}
		return null;
	}

	public static void RefreshAllControllerGlyphs()
	{
		foreach (MPEventSystem instances in instancesList)
		{
			if (instances.isActiveAndEnabled)
			{
				instances.RefreshControlGlyphs();
			}
		}
	}

	public void RefreshControlGlyphs()
	{
		OnLastActiveControllerChanged(player, player.controllers.GetLastActiveController());
	}

	protected override void Update()
	{
		EventSystem eventSystem = EventSystem.current;
		EventSystem.current = this;
		base.Update();
		EventSystem.current = eventSystem;
		ValidateCurrentSelectedGameobject();
		if (player.GetButtonDown(25) && (PauseScreenController.instancesList.Count == 0 || SimpleDialogBox.instancesList.Count == 0))
		{
			if (!PauseManager.isPaused)
			{
				PauseManager.PausingPlayerID = player.id;
			}
			if (PauseManager.PausingPlayerID == player.id)
			{
				Console.instance.SubmitCmd(null, "pause");
			}
		}
	}

	protected override void Awake()
	{
		base.Awake();
		instancesList.Add(this);
		cursorIndicatorController = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/CursorIndicator"), base.transform).GetComponent<CursorIndicatorController>();
		inputMapperHelper = new InputMapperHelper(this);
	}

	private void ValidateCurrentSelectedGameobject()
	{
		if ((bool)base.currentSelectedGameObject)
		{
			MPButton mPButton = (currentSelectedButton = base.currentSelectedGameObject.GetComponent<MPButton>());
			if ((bool)mPButton && (!mPButton.CanBeSelected() || (currentInputSource == InputSource.Gamepad && mPButton.navigation.mode == UnityEngine.UI.Navigation.Mode.None)))
			{
				SetSelectedGameObject(null);
				currentSelectedButton = null;
			}
		}
		else
		{
			currentSelectedButton = null;
		}
	}

	public void SetSelectedObject(GameObject o)
	{
		SetSelectedGameObject(o);
	}

	private static void OnActiveSceneChanged(Scene scene1, Scene scene2)
	{
		RecenterCursors();
	}

	private static void RecenterCursors()
	{
		foreach (MPEventSystem instances in instancesList)
		{
			if (instances.currentInputSource == InputSource.Gamepad && (bool)instances.currentInputModule)
			{
				((MPInput)instances.currentInputModule.input).CenterCursor();
			}
		}
	}

	protected override void OnDestroy()
	{
		player.controllers.RemoveLastActiveControllerChangedDelegate(OnLastActiveControllerChanged);
		instancesList.Remove(this);
		inputMapperHelper.Dispose();
		base.OnDestroy();
	}

	protected override void Start()
	{
		base.Start();
		SetCursorIndicatorEnabled(cursorIndicatorEnabled: false);
		if ((bool)base.currentInputModule && (bool)base.currentInputModule.input)
		{
			((MPInput)base.currentInputModule.input).CenterCursor();
		}
		player.controllers.AddLastActiveControllerChangedDelegate(OnLastActiveControllerChanged);
		OnLastActiveControllerChanged(player, player.controllers.GetLastActiveController());
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		activeCount++;
	}

	protected override void OnDisable()
	{
		SetCursorIndicatorEnabled(cursorIndicatorEnabled: false);
		base.OnDisable();
		if (activeCount > 0)
		{
			activeCount--;
		}
	}

	protected void LateUpdate()
	{
		bool flag = false;
		if ((bool)base.currentInputModule && (bool)base.currentInputModule.input)
		{
			flag = ((currentInputSource != InputSource.Gamepad) ? (cursorOpenerCount > 0) : (cursorOpenerForGamepadCount > 0));
		}
		SetCursorIndicatorEnabled(flag);
		MPInputModule mPInputModule = base.currentInputModule as MPInputModule;
		if (flag)
		{
			CursorIndicatorController.CursorSet cursorSet = cursorIndicatorController.noneCursorSet;
			switch (currentInputSource)
			{
			case InputSource.MouseAndKeyboard:
				cursorSet = cursorIndicatorController.mouseCursorSet;
				break;
			case InputSource.Gamepad:
				cursorSet = cursorIndicatorController.gamepadCursorSet;
				break;
			}
			cursorIndicatorController.SetCursor(cursorSet, (!isHovering) ? CursorIndicatorController.CursorImage.Pointer : CursorIndicatorController.CursorImage.Hover, GetColor());
			cursorIndicatorController.SetPosition(mPInputModule.input.mousePosition);
		}
	}

	private void OnLastActiveControllerChanged(Player player, Controller controller)
	{
		if (controller != null)
		{
			switch (controller.type)
			{
			case ControllerType.Keyboard:
				currentInputSource = InputSource.MouseAndKeyboard;
				break;
			case ControllerType.Mouse:
				currentInputSource = InputSource.MouseAndKeyboard;
				break;
			case ControllerType.Joystick:
				currentInputSource = InputSource.Gamepad;
				break;
			}
			if (InputBindingDisplayController.onBindingsChanged != null && controller != null)
			{
				InputBindingDisplayController.onBindingsChanged();
			}
		}
	}

	private void SetCursorIndicatorEnabled(bool cursorIndicatorEnabled)
	{
		if (cursorIndicatorController.gameObject.activeSelf != cursorIndicatorEnabled)
		{
			cursorIndicatorController.gameObject.SetActive(cursorIndicatorEnabled);
			if (cursorIndicatorEnabled)
			{
				((MPInput)((MPInputModule)base.currentInputModule).input).CenterCursor();
			}
		}
	}

	public Color GetColor()
	{
		if (activeCount <= 1)
		{
			return Color.white;
		}
		return ColorCatalog.GetMultiplayerColor(playerSlot);
	}

	public bool GetCursorPosition(out Vector2 position)
	{
		if ((bool)base.currentInputModule)
		{
			position = base.currentInputModule.input.mousePosition;
			return true;
		}
		position = Vector2.zero;
		return false;
	}

	public Rect GetScreenRect()
	{
		CameraRigController cameraRigController = localUser?.cameraRigController;
		if (!cameraRigController)
		{
			return new Rect(0f, 0f, Screen.width, Screen.height);
		}
		return cameraRigController.viewport;
	}

	private static Vector2 RandomOnCircle()
	{
		float value = UnityEngine.Random.value;
		return new Vector2(Mathf.Cos(value * MathF.PI * 2f), Mathf.Sin(value * MathF.PI * 2f));
	}

	private static Vector2 CalculateCursorPushVector(Vector2 positionA, Vector2 positionB)
	{
		Vector2 vector = positionA - positionB;
		if (vector == Vector2.zero)
		{
			vector = RandomOnCircle();
		}
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude >= 576f)
		{
			return Vector2.zero;
		}
		float num = Mathf.Sqrt(sqrMagnitude);
		float num2 = num * (1f / 24f);
		float num3 = 1f - num2;
		return vector / num * num3 * 10f * 0.5f;
	}

	private static void PushCursorsApart()
	{
		if (activeCount <= 1)
		{
			return;
		}
		int count = instancesList.Count;
		if (pushInfos.Length < activeCount)
		{
			pushInfos = new PushInfo[activeCount];
		}
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			if (instancesList[i].enabled)
			{
				instancesList[i].GetCursorPosition(out var position);
				pushInfos[num++] = new PushInfo
				{
					index = i,
					position = position
				};
			}
		}
		for (int j = 0; j < activeCount; j++)
		{
			PushInfo pushInfo = pushInfos[j];
			for (int k = j + 1; k < activeCount; k++)
			{
				PushInfo pushInfo2 = pushInfos[k];
				Vector2 vector = CalculateCursorPushVector(pushInfo.position, pushInfo2.position);
				pushInfo.pushVector += vector;
				pushInfo2.pushVector -= vector;
				pushInfos[k] = pushInfo2;
			}
			pushInfos[j] = pushInfo;
		}
		for (int l = 0; l < activeCount; l++)
		{
			PushInfo pushInfo3 = pushInfos[l];
			MPEventSystem mPEventSystem = instancesList[pushInfo3.index];
			if (mPEventSystem.allowCursorPush && (bool)mPEventSystem.currentInputModule)
			{
				((MPInput)mPEventSystem.currentInputModule.input).internalMousePosition += pushInfo3.pushVector;
			}
		}
	}

	static MPEventSystem()
	{
		instancesList = new List<MPEventSystem>();
		readOnlyInstancesList = new ReadOnlyCollection<MPEventSystem>(instancesList);
		pushInfos = Array.Empty<PushInfo>();
		RoR2Application.onUpdate += PushCursorsApart;
		SceneManager.activeSceneChanged += OnActiveSceneChanged;
	}
}
