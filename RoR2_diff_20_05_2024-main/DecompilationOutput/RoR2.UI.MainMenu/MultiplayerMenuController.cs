using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.MainMenu;

[RequireComponent(typeof(FirstSelectedObjectProvider))]
public class MultiplayerMenuController : BaseMainMenuScreen
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

	[Header("Subviews")]
	public GameObject MainMultiplayerMenu;

	public GameObject JoinGameMenu;

	public GameObject HostGameMenu;

	[Header("Buttons")]
	public MPButton hostGame;

	public MPButton findGame;

	public MPButton quickplayButton;

	public MPButton startPrivateGameButton;

	public MPButton inviteButton;

	public MPButton findFriendSessionButton;

	public MPButton backButton;

	public CarouselController gameModePicker;

	public MPButton enterGameButton;

	public GameObject[] lobbyActions;

	[Header("Quickplay Logic")]
	public LanguageTextMeshController quickplayLabelController;

	public string quickplayStartToken;

	public string quickplayStopToken;

	public RectTransform quickplaySpinnerRectTransform;

	public TextMeshProUGUI quickplayStateText;

	private string pendingQuickplayText;

	public RectTransform genericSpinnerRectTransform;

	private const float spinnerRotationSpeed = 360f;

	[Header("Sub-Views")]
	public SteamJoinClipboardLobby joinClipboardLobbyButtonController;

	public FriendSessionBrowserController friendSessionBrowserController;

	public LobbyUserList lobbyUserList;

	[Header("Platform Specific Components")]
	public CarouselController lobbyVisibilityController;

	public static float QuickplayWaitTime;

	public bool IsQuickPlaySearching;

	public bool IsQuickPlayButtonLocked;

	private Coroutine checkCanPlayOnlineCoroutine;

	public static MultiplayerMenuController instance { get; private set; }

	public Subview GetCurrentSubview()
	{
		return _curSubView;
	}

	public void SetSubview(Subview targetSubview)
	{
		_curSubView = targetSubview;
		MainMultiplayerMenu.SetActive(targetSubview == Subview.Main);
		JoinGameMenu.SetActive(targetSubview == Subview.FindGame);
		HostGameMenu.SetActive(targetSubview == Subview.HostGame);
		if ((bool)lobbyUserList)
		{
			lobbyUserList.DisplayContent(targetSubview == Subview.HostGame || targetSubview == Subview.Main);
		}
	}

	public void ReturnToMainSubview()
	{
		SetSubview(Subview.Main);
	}

	private void ToggleMPFeatures(MPFeatures featureFlags)
	{
		Toggle(hostGame, featureFlags.HasFlag(MPFeatures.HostGame));
		Toggle(findGame, featureFlags.HasFlag(MPFeatures.FindGame));
		Toggle(quickplayButton, featureFlags.HasFlag(MPFeatures.Quickplay));
		Toggle(gameModePicker, val: true);
		Toggle(startPrivateGameButton, featureFlags.HasFlag(MPFeatures.PrivateGame));
		Toggle(inviteButton, featureFlags.HasFlag(MPFeatures.Invite));
		Toggle(findFriendSessionButton, featureFlags.HasFlag(MPFeatures.FindFriendSession));
		Toggle(enterGameButton, featureFlags.HasFlag(MPFeatures.EnterGameButton));
	}

	private void ToggleMPFeaturesInteractable(MPFeatures featureFlags)
	{
		hostGame.interactable = featureFlags.HasFlag(MPFeatures.HostGame);
		findGame.interactable = featureFlags.HasFlag(MPFeatures.FindGame);
		quickplayButton.interactable = featureFlags.HasFlag(MPFeatures.Quickplay) && ShouldEnableQuickplayButton();
		gameModePicker.ToggleArrowsInteractable(isInteractable: true);
		startPrivateGameButton.interactable = featureFlags.HasFlag(MPFeatures.PrivateGame);
		inviteButton.interactable = featureFlags.HasFlag(MPFeatures.Invite);
		findFriendSessionButton.interactable = featureFlags.HasFlag(MPFeatures.FindFriendSession);
		if (enterGameButton != null)
		{
			enterGameButton.interactable = PlatformSystems.lobbyManager.ShouldEnableEnterGameButton();
		}
	}

	private void SetDefaultFallback()
	{
		if (quickplayButton.isActiveAndEnabled)
		{
			quickplayButton.defaultFallbackButton = true;
		}
	}

	private void ToggleMPLobbyFeaturesInteractable(MPLobbyFeatures lobbyFlags)
	{
		lobbyUserList.createLobbyButton.interactable = lobbyFlags.HasFlag(MPLobbyFeatures.CreateLobby);
		lobbyUserList.leaveLobbyButton.interactable = lobbyFlags.HasFlag(MPLobbyFeatures.LeaveLobby);
		lobbyUserList.copyLobbyButton.interactable = lobbyFlags.HasFlag(MPLobbyFeatures.Clipboard);
		lobbyUserList.joinLobbyButton.interactable = lobbyFlags.HasFlag(MPLobbyFeatures.Clipboard);
		lobbyUserList.lobbyTypeDropdown.interactable = lobbyFlags.HasFlag(MPLobbyFeatures.LobbyDropdownOptions);
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

	public void SetQuickplayButtonStateText(string textKey, object[] formatArgs = null)
	{
		if (formatArgs == null)
		{
			formatArgs = Array.Empty<object>();
		}
		TextMeshProUGUI textMeshProUGUI = quickplayStateText;
		if ((bool)textMeshProUGUI)
		{
			textMeshProUGUI.text = Language.GetStringFormatted(textKey, formatArgs);
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

	public void SetNetworkType(bool isInternet)
	{
		PlatformSystems.lobbyManager.SetNetworkType(isInternet);
	}

	public void RefreshFirstObjectSelectedProvider()
	{
		if ((bool)firstSelectedObjectProvider)
		{
			firstSelectedObjectProvider.firstSelectedObject = gameModePicker.gameObject;
			firstSelectedObjectProvider.fallBackFirstSelectedObjects = new GameObject[9] { gameModePicker.gameObject, hostGame.gameObject, findGame.gameObject, quickplayButton.gameObject, enterGameButton.gameObject, startPrivateGameButton.gameObject, inviteButton.gameObject, findFriendSessionButton.gameObject, backButton.gameObject };
		}
	}

	public new void OnEnable()
	{
		ResetText();
		ToggleMPFeatures(PlatformSystems.lobbyManager.GetPlatformMPFeatureFlags());
		SetupLocalAndInternetMPButtons();
		if ((bool)lobbyUserList)
		{
			lobbyUserList.DisplayContent(display: true);
		}
		RefreshFirstObjectSelectedProvider();
		BuildGameModeChoices();
		SetDefaultFallback();
		firstSelectedObjectProvider?.ForceSelectFirstInteractable();
		LerpAllUI(LerpUIRect.LerpState.Entering);
		if (!instance)
		{
			instance = SingletonHelper.Assign(instance, this);
		}
		PlatformSystems.lobbyManager.OnMultiplayerMenuEnabled(OnLobbyLeave);
		checkCanPlayOnlineCoroutine = StartCoroutine(CheckCanPlayOnline());
		firstSelectedObjectProvider?.ResetLastSelected();
		firstSelectedObjectProvider?.EnsureSelectedObject();
	}

	private IEnumerator CheckCanPlayOnline()
	{
		IEnumerator<NetworkManagerSystem.CanPlayOnlineState> canPlayOnlineEnumerator = NetworkManagerSystem.singleton.CanPlayOnline();
		NetworkManagerSystem.CanPlayOnlineState lastResult = NetworkManagerSystem.CanPlayOnlineState.Pending;
		while (canPlayOnlineEnumerator.MoveNext())
		{
			lastResult = canPlayOnlineEnumerator.Current;
			if (lastResult != 0)
			{
				break;
			}
			yield return null;
		}
		if (lastResult != NetworkManagerSystem.CanPlayOnlineState.Yes)
		{
			if (!NetworkManagerSystem.singleton.isNetworkActive)
			{
				PlatformSystems.lobbyManager.LeaveLobby();
			}
			myMainMenuController.SetDesiredMenuScreen(myMainMenuController.titleMenuScreen);
		}
		checkCanPlayOnlineCoroutine = null;
	}

	public void OnDisable()
	{
		if (checkCanPlayOnlineCoroutine != null)
		{
			StopCoroutine(checkCanPlayOnlineCoroutine);
		}
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyLeave = (Action<PlatformID>)Delegate.Remove(lobbyManager.onLobbyLeave, new Action<PlatformID>(OnLobbyLeave));
		if (!NetworkManagerSystem.singleton.isNetworkActive)
		{
			PlatformSystems.lobbyManager.LeaveLobby();
		}
		instance = SingletonHelper.Unassign(instance, this);
	}

	private void OnLobbyLeave(PlatformID lobbyId)
	{
		ResetText();
		if (!(PlatformSystems.lobbyManager as SteamworksLobbyManager).isInLobbyDelayed && !PlatformSystems.lobbyManager.awaitingJoin && MainMenuController.instance.desiredMenuScreen == MainMenuController.instance.multiplayerMenuScreen)
		{
			PlatformSystems.lobbyManager.CreateLobby();
		}
	}

	public new void Awake()
	{
		base.Awake();
		LerpAllUI(LerpUIRect.LerpState.Entering);
		hostGame.onClick.AddListener(delegate
		{
			SetSubview(Subview.HostGame);
		});
		findGame.onClick.AddListener(delegate
		{
			SetSubview(Subview.FindGame);
		});
		quickplayButton.onClick.AddListener(delegate
		{
			PlatformSystems.lobbyManager.ToggleQuickplay();
		});
		startPrivateGameButton.onClick.AddListener(delegate
		{
			PlatformSystems.lobbyManager.OnStartPrivateGame();
		});
		findFriendSessionButton.onClick.AddListener(delegate
		{
			PlatformSystems.lobbyManager.OpenFriendSessionBrowser();
		});
	}

	public void ShowFriendSessionBrowser()
	{
		friendSessionBrowserController.gameObject.SetActive(value: true);
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
		gameModePicker.gameObject.SetActive(list.Count > 1);
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

	public void ResetText()
	{
		if (PlatformSystems.lobbyManager.GetPlatformMPFeatureFlags().HasFlag(MPFeatures.Quickplay))
		{
			SetQuickplayText("", new object[0]);
		}
	}

	private void SetupLocalAndInternetMPButtons()
	{
		LanguageTextMeshController componentInChildren = findFriendSessionButton.GetComponentInChildren<LanguageTextMeshController>();
		LanguageTextMeshController languageTextMeshController = (inviteButton as HGButton)?.hoverLanguageTextMeshController;
		LanguageTextMeshController componentInChildren2 = startPrivateGameButton.GetComponentInChildren<LanguageTextMeshController>();
		if (PlatformSystems.lobbyManager.IsNetworkTypeInternet())
		{
			if ((bool)componentInChildren)
			{
				componentInChildren.token = "FRIEND_GAME_BROWSER";
			}
			if ((bool)languageTextMeshController)
			{
				languageTextMeshController.token = "FRIEND_GAME_BROWSER_DESCRIPTION";
			}
			if ((bool)componentInChildren2)
			{
				componentInChildren2.token = "HOST_PRIVATE_GAME";
			}
			return;
		}
		enterGameButton.interactable = false;
		if ((bool)componentInChildren)
		{
			componentInChildren.token = "BROWSE_LOCAL_GAME";
		}
		if ((bool)languageTextMeshController)
		{
			languageTextMeshController.token = "BROWSE_LOCAL_GAME_DESCRIPTION";
		}
		if ((bool)componentInChildren2)
		{
			componentInChildren2.token = "HOST_LOCAL_GAME";
		}
	}

	public void ReturnToMainMenu()
	{
		ResetLobby();
		myMainMenuController.SetDesiredMenuScreen(myMainMenuController.titleMenuScreen);
	}

	public void ResetLobby()
	{
		PlatformSystems.lobbyManager.LeaveLobby();
		if (enterGameButton != null)
		{
			enterGameButton.interactable = PlatformSystems.lobbyManager.ShouldEnableEnterGameButton();
		}
		lobbyUserList.ClearUserList();
	}

	public void OnEnterGameButtonPressed()
	{
		titleStopwatch = 0f;
		LerpAllUI(LerpUIRect.LerpState.Leaving);
		PlatformSystems.lobbyManager.EnterGameButtonPressed();
	}

	private void UpdateSpinner(RectTransform spinnerRectTransform)
	{
		Vector3 localEulerAngles = spinnerRectTransform.localEulerAngles;
		localEulerAngles.z += Time.deltaTime * 360f;
		spinnerRectTransform.localEulerAngles = localEulerAngles;
	}

	private new void Update()
	{
		base.Update();
		titleStopwatch += Time.deltaTime;
		PlatformSystems.lobbyManager.CheckBusyTimer();
		if (friendSessionBrowserController.gameObject.activeSelf)
		{
			return;
		}
		if (PlatformSystems.lobbyManager.IsQuickPlaySearching())
		{
			ToggleMPFeaturesInteractable(MPFeatures.Quickplay);
			quickplayLabelController.token = quickplayStopToken;
		}
		if (PlatformSystems.lobbyManager.ShouldShowSpinner())
		{
			quickplaySpinnerRectTransform.gameObject.SetActive(value: true);
			UpdateSpinner(quickplaySpinnerRectTransform);
		}
		else
		{
			ToggleMPFeaturesInteractable(PlatformSystems.lobbyManager.GetPlatformMPFeatureFlags());
			quickplaySpinnerRectTransform.gameObject.SetActive(value: false);
			if (!PlatformSystems.lobbyManager.IsQuickPlaySearching())
			{
				quickplayLabelController.token = quickplayStartToken;
			}
			else
			{
				quickplayLabelController.token = quickplayStopToken;
			}
			if ((bool)quickplayButton)
			{
				quickplayButton.interactable = ShouldEnableQuickplayButton();
			}
			else
			{
				quickplayButton.interactable = ShouldEnableQuickplayButton();
				if (!ShouldEnableQuickplayButton())
				{
					quickplaySpinnerRectTransform.gameObject.SetActive(value: true);
					UpdateSpinner(quickplaySpinnerRectTransform);
				}
			}
			if ((bool)startPrivateGameButton)
			{
				startPrivateGameButton.interactable = ShouldEnableStartPrivateGameButton();
			}
			if ((bool)joinClipboardLobbyButtonController && (bool)joinClipboardLobbyButtonController.mpButton)
			{
				joinClipboardLobbyButtonController.mpButton.interactable = ShouldEnableJoinClipboardLobbyButton();
			}
			if ((bool)inviteButton)
			{
				inviteButton.interactable = PlatformSystems.lobbyManager.ShouldEnableInviteButton();
			}
			if ((bool)findFriendSessionButton)
			{
				findFriendSessionButton.interactable = ShouldEnableFindFriendSessionButton();
			}
			backButton.interactable = ShouldEnableBackButton();
			if (enterGameButton != null && enterGameButton.isActiveAndEnabled)
			{
				enterGameButton.interactable = PlatformSystems.lobbyManager.ShouldEnableEnterGameButton();
			}
		}
		if (PlatformSystems.lobbyManager.ShouldShowGenericSpinner())
		{
			genericSpinnerRectTransform.gameObject.SetActive(value: true);
			UpdateSpinner(genericSpinnerRectTransform);
		}
		else
		{
			genericSpinnerRectTransform.gameObject.SetActive(value: false);
		}
		if (!string.IsNullOrEmpty(pendingQuickplayText))
		{
			SetQuickplayText(pendingQuickplayText);
			pendingQuickplayText = string.Empty;
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

	public void InviteButtonPressed()
	{
		if (!PlatformSystems.lobbyManager.isInLobby)
		{
			NetworkManagerSystem.singleton.CreateLocalLobby();
		}
		PlatformSystems.lobbyManager.OpenInviteOverlay();
	}

	private bool ShouldEnableQuickplayButton()
	{
		return PlatformSystems.lobbyManager.ShouldEnableQuickplayButton();
	}

	private bool ShouldEnableStartPrivateGameButton()
	{
		return PlatformSystems.lobbyManager.ShouldEnableStartPrivateGameButton();
	}

	private bool ShouldEnableJoinClipboardLobbyButton()
	{
		if (!PlatformSystems.lobbyManager.newestLobbyData.quickplayQueued)
		{
			return joinClipboardLobbyButtonController.validClipboardLobbyID;
		}
		return false;
	}

	private bool ShouldEnableFindFriendSessionButton()
	{
		return PlatformSystems.lobbyManager.ShouldEnableFindFriendSessionButton();
	}

	public void ToggleQuickplay()
	{
		PlatformSystems.lobbyManager.ToggleQuickplay();
	}

	public override bool IsReadyToLeave()
	{
		return true;
	}

	public bool ShouldEnableBackButton()
	{
		if (!PlatformSystems.lobbyManager.IsBusy)
		{
			return _curSubView == Subview.Main;
		}
		return false;
	}

	public void GoBack()
	{
		if (friendSessionBrowserController.isActiveAndEnabled)
		{
			friendSessionBrowserController.gameObject.SetActive(value: false);
		}
		else if (PlatformSystems.lobbyManager.IsQuickPlaySearching() && NetworkManagerSystem.singleton.IsHost())
		{
			quickplayButton.InvokeClick();
		}
		else if (PlatformSystems.lobbyManager.isInLobby)
		{
			ResetLobby();
			ResetLastSelected();
			ReturnToMainMenu();
		}
		else if (ShouldEnableBackButton())
		{
			ReturnToMainMenu();
		}
	}

	public void ResetLastSelected()
	{
		firstSelectedObjectProvider?.ResetLastSelected();
		firstSelectedObjectProvider?.EnsureSelectedObject();
	}

	public void SetQuickplayTextBelowMinPlayers()
	{
		if (PlatformSystems.lobbyManager.HasMPFeature(MPFeatures.Quickplay))
		{
			object[] formatArgs = new object[2]
			{
				PlatformSystems.lobbyManager.calculatedTotalPlayerCount,
				RoR2Application.maxPlayers
			};
			SetQuickplayText("STEAM_LOBBY_STATUS_QUICKPLAY_WAITING_BELOW_MINIMUM_PLAYERS", formatArgs);
		}
	}

	public void UpdateCutoffTimeText(int numberOfPlayers, float timeRemaining)
	{
		object[] formatArgs = new object[3]
		{
			numberOfPlayers,
			RoR2Application.maxPlayers,
			(int)Math.Max(0.0, timeRemaining)
		};
		SetQuickplayText("STEAM_LOBBY_STATUS_QUICKPLAY_WAITING_ABOVE_MINIMUM_PLAYERS", formatArgs);
	}

	public void SetPendingQuickplayText(string text, object[] formatArgs = null)
	{
		if (formatArgs == null)
		{
			formatArgs = Array.Empty<object>();
		}
		pendingQuickplayText = Language.GetStringFormatted(text, formatArgs);
	}

	public void SetQuickplayText(string text, object[] formatArgs = null)
	{
		if (formatArgs == null)
		{
			formatArgs = Array.Empty<object>();
		}
		quickplayStateText.text = Language.GetStringFormatted(text, formatArgs);
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
		case MPButtons.HostGame:
			mPButton = hostGame;
			break;
		case MPButtons.FindGame:
			mPButton = findGame;
			break;
		case MPButtons.Quickplay:
			mPButton = quickplayButton;
			break;
		case MPButtons.StartPrivateGame:
			mPButton = startPrivateGameButton;
			break;
		case MPButtons.Invite:
			mPButton = inviteButton;
			break;
		case MPButtons.FindFriendSession:
			mPButton = findFriendSessionButton;
			break;
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
