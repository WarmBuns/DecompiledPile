using System;
using System.Collections.Generic;
using System.Linq;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.MainMenu;

[RequireComponent(typeof(FirstSelectedObjectProvider))]
public class LocalMultiplayerMenuController : BaseMainMenuScreen
{
	public enum Subview
	{
		Main,
		FindGame,
		HostGame
	}

	public enum MPButtons
	{
		HostGame,
		FindGame,
		Quickplay,
		StartPrivateGame,
		Invite,
		FindFriendSession,
		Back,
		EnterGame,
		NONE
	}

	private const float titleTransitionDuration = 0.5f;

	private const float titleTransitionBuffer = 0.1f;

	public Image fadeImage;

	public LerpUIRect[] uiToLerp;

	private float titleStopwatch;

	private Subview _curSubView;

	[Header("Buttons")]
	public MPButton backButton;

	public CarouselController gameModePicker;

	public MPButton enterGameButton;

	public static LocalMultiplayerMenuController instance { get; private set; }

	private void ToggleMPFeatures(MPFeatures featureFlags)
	{
		Toggle(gameModePicker, val: true);
		Toggle(enterGameButton, val: true);
	}

	private void ToggleMPFeaturesInteractable(MPFeatures featureFlags)
	{
		gameModePicker.ToggleArrowsInteractable(isInteractable: true);
		if (enterGameButton != null)
		{
			enterGameButton.interactable = true;
		}
	}

	private void Toggle(MonoBehaviour component, bool val)
	{
		if ((bool)component)
		{
			component.gameObject.SetActive(val);
		}
		else
		{
			Debug.LogError("Null ref on Toggle of Button in MultiplayerMenuController on \"" + base.gameObject.name + "\"");
		}
	}

	private void Toggle(GameObject[] goToToggle, bool val)
	{
		int num = goToToggle.Length;
		for (int i = 0; i < num; i++)
		{
			goToToggle[i].gameObject.SetActive(val);
		}
	}

	private void Toggle(MonoBehaviour[] buttons, bool val)
	{
		int num = buttons.Length;
		for (int i = 0; i < num; i++)
		{
			buttons[i].gameObject.SetActive(val);
		}
	}

	public void SetButtonText(MPButton button, string textKey, object[] formatArgs = null)
	{
		if (formatArgs == null)
		{
			formatArgs = Array.Empty<object>();
		}
		TextMeshProUGUI component = button.GetComponent<TextMeshProUGUI>();
		if ((bool)component)
		{
			component.text = Language.GetStringFormatted(textKey, formatArgs);
		}
	}

	public void RefreshFirstObjectSelectedProvider()
	{
		if ((bool)firstSelectedObjectProvider)
		{
			firstSelectedObjectProvider.firstSelectedObject = gameModePicker.gameObject;
			firstSelectedObjectProvider.fallBackFirstSelectedObjects = new GameObject[3] { gameModePicker.gameObject, enterGameButton.gameObject, backButton.gameObject };
		}
	}

	public new void OnEnable()
	{
		RefreshFirstObjectSelectedProvider();
		firstSelectedObjectProvider?.ForceSelectFirstInteractable();
		LerpAllUI(LerpUIRect.LerpState.Entering);
		if (!instance)
		{
			instance = SingletonHelper.Assign(instance, this);
		}
		firstSelectedObjectProvider?.ResetLastSelected();
		firstSelectedObjectProvider?.EnsureSelectedObject();
	}

	public void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}

	public new void Awake()
	{
		base.Awake();
		LerpAllUI(LerpUIRect.LerpState.Entering);
		BuildGameModeChoices();
	}

	public void LerpAllUI(LerpUIRect.LerpState lerpState)
	{
		LerpUIRect[] array = uiToLerp;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].lerpState = lerpState;
		}
	}

	private void BuildGameModeChoices()
	{
		List<CarouselController.Choice> list = new List<CarouselController.Choice>();
		List<string> list2 = gameModePicker.choices.Select((CarouselController.Choice choice) => choice.suboptionDisplayToken).ToList();
		for (GameModeIndex gameModeIndex = (GameModeIndex)0; (int)gameModeIndex < GameModeCatalog.gameModeCount; gameModeIndex++)
		{
			Run gameModePrefabComponent = GameModeCatalog.GetGameModePrefabComponent(gameModeIndex);
			ExpansionRequirementComponent component = gameModePrefabComponent.GetComponent<ExpansionRequirementComponent>();
			if (gameModePrefabComponent != null && gameModePrefabComponent.userPickable && list2.Contains(gameModePrefabComponent.nameToken) && (!component || !component.requiredExpansion || EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(component.requiredExpansion.requiredEntitlement)))
			{
				list.Add(new CarouselController.Choice
				{
					suboptionDisplayToken = gameModePrefabComponent.nameToken,
					convarValue = gameModePrefabComponent.name
				});
			}
		}
		gameModePicker.choices = list.ToArray();
		gameModePicker.gameObject.SetActive(list.Count > 0);
		string @string = Console.instance.FindConVar("gamemode").GetString();
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].convarValue == @string)
			{
				flag = true;
				break;
			}
		}
		if (list.Count == 1 || !flag)
		{
			Debug.LogFormat("Invalid gamemode {0} detected. Reverting to ClassicRun.", @string);
			gameModePicker.SubmitSetting(list[0].convarValue);
		}
	}

	public void ReturnToMainMenu()
	{
		myMainMenuController.SetDesiredMenuScreen(myMainMenuController.titleMenuScreen);
	}

	public void OnEnterGameButtonPressed()
	{
		titleStopwatch = 0f;
		LerpAllUI(LerpUIRect.LerpState.Leaving);
	}

	public void OnStartGamePressed()
	{
		string @string = Console.instance.FindConVar("gamemode").GetString();
		Console.instance.SubmitCmd(null, $"transition_command \"gamemode {@string}; host 0;\"");
	}

	private new void Update()
	{
		base.Update();
		titleStopwatch += Time.deltaTime;
		if (enterGameButton != null && enterGameButton.isActiveAndEnabled)
		{
			enterGameButton.interactable = true;
		}
		if (SimpleDialogBox.instancesList.Count > 0 && (bool)firstSelectedObjectProvider)
		{
			SimpleDialogBox simpleDialogBox = SimpleDialogBox.instancesList[0];
			if ((bool)simpleDialogBox)
			{
				firstSelectedObjectProvider.firstSelectedObject = simpleDialogBox.GetComponentInChildren<MPButton>().gameObject;
				firstSelectedObjectProvider.ResetLastSelected();
			}
		}
		firstSelectedObjectProvider?.EnsureSelectedObject();
	}

	public override bool IsReadyToLeave()
	{
		return true;
	}

	public void GoBack()
	{
		ReturnToMainMenu();
	}

	public void ResetLastSelected()
	{
		firstSelectedObjectProvider?.ResetLastSelected();
		firstSelectedObjectProvider?.EnsureSelectedObject();
	}

	public void ToggleAllButtonsInteractable(bool interactable)
	{
		for (int i = 0; i < 8; i++)
		{
			SetButtonInteractableStatus((MPButtons)i, interactable);
		}
		firstSelectedObjectProvider.ForceSelectFirstInteractable();
	}

	public void SetButtonInteractableStatus(MPButtons button, bool interactable)
	{
		MPButton mPButton = null;
		switch (button)
		{
		case MPButtons.Back:
			mPButton = backButton;
			break;
		case MPButtons.EnterGame:
			mPButton = enterGameButton;
			break;
		}
		if (mPButton != null)
		{
			mPButton.interactable = interactable;
		}
	}
}
