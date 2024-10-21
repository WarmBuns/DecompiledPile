using RoR2.EntityLogic;
using UnityEngine;

namespace RoR2.UI.MainMenu;

public class TitleMenuController : MonoBehaviour
{
	public ConsoleFunctions consoleFunctions;

	public MainMenuController mainMenuController;

	public DelayedEvent titleDelayedEvent;

	public CanvasGroup titleButtonPanelCanvas;

	public MultiplayerMenuController multiplayerMenuController;

	public void OnSinglePlayerButtonPressed()
	{
		if (CanTransitionOutOfTitleMenu())
		{
			consoleFunctions.SubmitCmd("transition_command \"gamemode ClassicRun; host 0;\"");
			titleDelayedEvent.CallDelayed(0f);
			titleButtonPanelCanvas.interactable = false;
			mainMenuController.SetAllowTransition(value: false);
		}
	}

	public void OnMultiplayerButtonPressed()
	{
		if (CanTransitionOutOfTitleMenu() && RoR2Application.IsAllUsersEntitlementsUpdated)
		{
			multiplayerMenuController.SetNetworkType(isInternet: true);
			mainMenuController.SetDesiredMenuScreen(mainMenuController.multiplayerMenuScreen);
		}
	}

	public void OnExtraGameModeButtonPressed()
	{
		if (CanTransitionOutOfTitleMenu())
		{
			mainMenuController.SetDesiredMenuScreen(mainMenuController.extraGameModeMenuScreen);
		}
	}

	public void OnLogbookButtonPressed()
	{
		if (CanTransitionOutOfTitleMenu())
		{
			consoleFunctions.SubmitCmd("transition_command \"set_scene logbook\"");
			titleDelayedEvent.CallDelayed(0f);
		}
	}

	private bool CanTransitionOutOfTitleMenu()
	{
		if (RoR2Application.IsMainMenuInitialized && RoR2Application.IsEntitlementsUpdated)
		{
			return !FadeToBlackManager.IsFading();
		}
		return false;
	}
}
