using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class InputBindingControl : MonoBehaviour
{
	public string actionName;

	public AxisRange axisRange;

	public LanguageTextMeshController nameLabel;

	public InputBindingDisplayController bindingDisplay;

	public MPEventSystem.InputSource inputSource;

	public MPButton button;

	private MPEventSystemLocator eventSystemLocator;

	private InputAction action;

	private InputMapperHelper inputMapperHelper;

	private Player currentPlayer;

	private float buttonReactivationTime;

	private bool isListening => inputMapperHelper.isListening;

	public void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		bindingDisplay.actionName = actionName;
		bindingDisplay.useExplicitInputSource = true;
		bindingDisplay.explicitInputSource = inputSource;
		bindingDisplay.axisRange = axisRange;
		nameLabel.token = InputCatalog.GetActionNameToken(actionName, axisRange);
		action = ReInput.mapping.GetAction(actionName);
	}

	public void ToggleListening()
	{
		if (!isListening)
		{
			StartListening();
		}
		else
		{
			StopListening();
		}
	}

	public void StartListening()
	{
		if (!button.IsInteractable())
		{
			return;
		}
		inputMapperHelper.Stop();
		currentPlayer = eventSystemLocator.eventSystem?.localUser?.inputPlayer;
		if (currentPlayer != null)
		{
			IList<Controller> controllers = null;
			switch (inputSource)
			{
			case MPEventSystem.InputSource.MouseAndKeyboard:
				controllers = new Controller[2]
				{
					currentPlayer.controllers.Keyboard,
					currentPlayer.controllers.Mouse
				};
				break;
			case MPEventSystem.InputSource.Gamepad:
				controllers = currentPlayer.controllers.Joysticks.ToArray();
				break;
			}
			inputMapperHelper.Start(currentPlayer, controllers, action, axisRange);
			if ((bool)button)
			{
				button.interactable = false;
			}
		}
	}

	private void StopListening()
	{
		if (currentPlayer != null)
		{
			currentPlayer = null;
			inputMapperHelper.Stop();
		}
	}

	private void OnEnable()
	{
		if (!eventSystemLocator.eventSystem)
		{
			base.enabled = false;
		}
		else
		{
			inputMapperHelper = eventSystemLocator.eventSystem.inputMapperHelper;
		}
	}

	private void OnDisable()
	{
		StopListening();
	}

	private void Update()
	{
		if (!button)
		{
			return;
		}
		if (!(eventSystemLocator?.eventSystem))
		{
			Debug.LogError("MPEventSystem is invalid.");
			return;
		}
		bool flag = !eventSystemLocator.eventSystem.inputMapperHelper.isListening;
		if (!flag)
		{
			buttonReactivationTime = Time.unscaledTime + 0.25f;
		}
		button.interactable = flag && buttonReactivationTime <= Time.unscaledTime;
	}
}
