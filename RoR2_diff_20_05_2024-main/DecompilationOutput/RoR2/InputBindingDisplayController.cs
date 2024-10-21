using System;
using System.Collections;
using Rewired;
using RoR2.UI;
using TMPro;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(MPEventSystemLocator))]
public class InputBindingDisplayController : MonoBehaviour
{
	public string actionName;

	private bool needsUpdate = true;

	public AxisRange axisRange;

	public bool useExplicitInputSource;

	public MPEventSystem.InputSource explicitInputSource;

	public bool allowFallbackInputSource;

	private MPEventSystemLocator eventSystemLocator;

	private TextMeshProUGUI guiLabel;

	private TextMeshPro label;

	public Action oneFrameAfterTextUpdate;

	public static Action onBindingsChanged;

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		guiLabel = GetComponent<TextMeshProUGUI>();
		label = GetComponent<TextMeshPro>();
	}

	private void OnEnable()
	{
		onBindingsChanged = (Action)Delegate.Combine(onBindingsChanged, new Action(Refresh));
		Refresh();
	}

	private void OnDisable()
	{
		onBindingsChanged = (Action)Delegate.Remove(onBindingsChanged, new Action(Refresh));
	}

	private void Start()
	{
		Refresh();
	}

	private void Refresh()
	{
		if (!(eventSystemLocator?.eventSystem))
		{
			Debug.LogError("MPEventSystem is invalid.");
			return;
		}
		bool flag = useExplicitInputSource;
		if (useExplicitInputSource && allowFallbackInputSource)
		{
			Player player = ReInput.players.GetPlayer(0);
			if (player != null)
			{
				Controller lastActiveController = player.controllers.GetLastActiveController();
				if (lastActiveController != null)
				{
					switch (explicitInputSource)
					{
					case MPEventSystem.InputSource.Gamepad:
						flag = lastActiveController.type == ControllerType.Joystick;
						break;
					case MPEventSystem.InputSource.MouseAndKeyboard:
						flag = lastActiveController.type == ControllerType.Mouse || lastActiveController.type == ControllerType.Keyboard;
						break;
					}
				}
			}
		}
		string empty = string.Empty;
		empty = ((!flag) ? Glyphs.GetGlyphString(eventSystemLocator.eventSystem, actionName) : Glyphs.GetGlyphString(eventSystemLocator.eventSystem, actionName, axisRange, explicitInputSource));
		if (!string.IsNullOrEmpty(empty))
		{
			if ((bool)guiLabel)
			{
				guiLabel.text = empty;
				StartCoroutine(InvokeOneFrameAfterTextUpdateCallback());
			}
			else if ((bool)label)
			{
				label.text = empty;
				StartCoroutine(InvokeOneFrameAfterTextUpdateCallback());
			}
		}
	}

	private IEnumerator InvokeOneFrameAfterTextUpdateCallback()
	{
		yield return null;
		oneFrameAfterTextUpdate?.Invoke();
	}
}
