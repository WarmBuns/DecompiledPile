using System;
using System.Collections;
using Assets.RoR2.Scripts.Platform;
using RoR2.ConVar;
using RoR2.Networking;
using RoR2.UI.LogBook;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.UI.MainMenu;

public sealed class MainMenuController : MonoBehaviour
{
	[NonSerialized]
	public BaseMainMenuScreen desiredMenuScreen;

	public BaseMainMenuScreen LoadingScreen;

	public BaseMainMenuScreen profileMenuScreen;

	public BaseMainMenuScreen EAwarningProfileMenu;

	public BaseMainMenuScreen multiplayerMenuScreen;

	public BaseMainMenuScreen localMultiplayerMenuScreen;

	public BaseMainMenuScreen titleMenuScreen;

	public BaseMainMenuScreen settingsMenuScreen;

	public BaseMainMenuScreen moreMenuScreen;

	public BaseMainMenuScreen extraGameModeMenuScreen;

	[HideInInspector]
	public BaseMainMenuScreen currentMenuScreen;

	public static MainMenuController instance;

	public HGButton exitButtonTransition;

	public HGButton profileButtonTransition;

	public HGButton onlineMultiplayerButtonTransition;

	public HGButton localMultiplayerButtonTransition;

	public HGButton splitscreenMultiplayerButtonTransition;

	public HGButton logbookButton;

	public HGButton singlePlayerButton;

	public GameObject steamBuildLabel;

	[NonSerialized]
	public bool allowTransition = true;

	public HGButton continueButtonTransition;

	public GameObject EA_Panel;

	public GameObject mainMenuButtonPanel;

	public Transform cameraTransform;

	public float camRotationSmoothDampTime;

	public float camTranslationSmoothDampTime;

	private Vector3 camSmoothDampPositionVelocity;

	private Vector3 camSmoothDampRotationVelocity;

	public float camTransitionDuration;

	private float camTransitionTimer;

	private static bool isShownAgeRestriction = false;

	private static bool wasInMultiplayer = false;

	private static EOSLoginManager.EOSLoginState lastEOSLoginState = EOSLoginManager.EOSLoginState.None;

	private static bool eaWarningShown = false;

	private static BoolConVar eaMessageSkipConVar = new BoolConVar("ea_message_skip", ConVarFlags.None, "0", "Whether or not to skip the early access splash screen.");

	private static bool _isInitialized = false;

	public static bool IsOnMultiplayerScreen { get; set; } = false;

	private static bool isInitialized
	{
		get
		{
			return _isInitialized;
		}
		set
		{
			_isInitialized = value;
			if (_isInitialized)
			{
				MainMenuController.OnMainMenuInitialised?.Invoke();
			}
		}
	}

	public static event Action OnMainMenuInitialised;

	public static event Action OnPreMainMenuInitialized;

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		NetworkManagerSystem.onStartClientGlobal += delegate
		{
			if (!NetworkServer.active || !NetworkServer.dontListen)
			{
				wasInMultiplayer = true;
			}
		};
	}

	private void Awake()
	{
		RoR2Application.onStart = (Action)Delegate.Combine(RoR2Application.onStart, new Action(StartManaged));
		PauseManager.InitializeLoad();
		RoR2Application.isInLocalMultiPlayer = false;
	}

	private void OnDestroy()
	{
	}

	public void SetButtonsInteractible(bool enabled)
	{
		exitButtonTransition.interactable = enabled;
		profileButtonTransition.interactable = enabled;
		onlineMultiplayerButtonTransition.interactable = enabled;
		localMultiplayerButtonTransition.interactable = enabled;
		splitscreenMultiplayerButtonTransition.interactable = enabled;
		logbookButton.interactable = enabled;
	}

	private void OnNetworkReachabilityChanged(bool networkReachability)
	{
		if (networkReachability)
		{
			SetupMPFeatureButtons();
		}
	}

	private void Start()
	{
		if (instance != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		SetupMPFeatureButtons();
		exitButtonTransition.transform.SetAsLastSibling();
		StartWaitForLoad();
		if ((bool)LoadingScreen)
		{
			LoadingScreen.gameObject.SetActive(value: true);
		}
		titleMenuScreen.gameObject.SetActive(value: false);
		multiplayerMenuScreen.gameObject.SetActive(value: false);
		localMultiplayerMenuScreen.gameObject.SetActive(value: false);
		settingsMenuScreen.gameObject.SetActive(value: false);
		moreMenuScreen.gameObject.SetActive(value: false);
		extraGameModeMenuScreen.gameObject.SetActive(value: false);
		if (GameIntentReceiver.GameIntentAdapter.hasPendingGameIntent)
		{
			FadeToBlackManager.fadePaused = true;
		}
		desiredMenuScreen = (wasInMultiplayer ? multiplayerMenuScreen : titleMenuScreen);
		_ = wasInMultiplayer;
		wasInMultiplayer = false;
		StartManaged();
	}

	private void DisableButtonsForConsole()
	{
	}

	private void SetupMPFeatureButtons()
	{
		bool flag = PlatformSystems.lobbyManager.GetPlatformMPFeatureFlags().HasFlag(MPFeatures.AdHoc);
		localMultiplayerButtonTransition.gameObject.SetActive(flag);
		LanguageTextMeshController componentInChildren = onlineMultiplayerButtonTransition.GetComponentInChildren<LanguageTextMeshController>();
		if ((bool)componentInChildren)
		{
			componentInChildren.token = (flag ? "TITLE_MULTIPLAYER_ONLINE" : "TITLE_MULTIPLAYER");
		}
		splitscreenMultiplayerButtonTransition.gameObject.SetActive(value: false);
	}

	private void DialogBox(string headerToken, string descriptionToken)
	{
		if (SimpleDialogBox.instancesList.Count == 0)
		{
			SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
			simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair(headerToken);
			simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair(descriptionToken);
			simpleDialogBox.AddCancelButton(CommonLanguageTokens.ok);
			simpleDialogBox.rootObject.transform.SetParent(RoR2Application.instance.mainCanvas.transform);
		}
	}

	private void StartManaged()
	{
		CheckWarningScreen();
		currentMenuScreen = desiredMenuScreen;
		if (!currentMenuScreen.gameObject.activeInHierarchy)
		{
			currentMenuScreen.gameObject.SetActive(value: true);
		}
		if (currentMenuScreen.desiredCameraTransform != null)
		{
			cameraTransform.rotation = currentMenuScreen.desiredCameraTransform.rotation;
		}
		if ((bool)currentMenuScreen)
		{
			currentMenuScreen.OnEnter(this);
		}
		IsOnMultiplayerScreen = currentMenuScreen == multiplayerMenuScreen;
	}

	private bool CheckWarningScreen()
	{
		return false;
	}

	private static bool IsMainUserSignedIn()
	{
		return LocalUserManager.FindLocalUser(0) != null;
	}

	private bool IsInLobby()
	{
		return PlatformSystems.lobbyManager.isInLobby;
	}

	private void Update()
	{
		if (isInitialized)
		{
			if (currentMenuScreen == multiplayerMenuScreen && PlayerPrefs.GetInt("HasShownFirstTimePopup", 0) == 0)
			{
				PlayerPrefs.SetInt("HasShownFirstTimePopup", 1);
				ShowFirstTimeCrossPlayPopup();
			}
			if (IsInLobby() && currentMenuScreen != multiplayerMenuScreen && currentMenuScreen != localMultiplayerMenuScreen)
			{
				desiredMenuScreen = multiplayerMenuScreen;
			}
			if (!IsMainUserSignedIn() && currentMenuScreen != EAwarningProfileMenu)
			{
				desiredMenuScreen = profileMenuScreen;
			}
			if (!(currentMenuScreen == null))
			{
				UpdateMenuTransition();
			}
		}
	}

	private void UpdateMenuTransition()
	{
		if (desiredMenuScreen != currentMenuScreen)
		{
			if (desiredMenuScreen == multiplayerMenuScreen && PlatformSystems.userManager.CurrentUserHasRestriction(UserManager.PlatformRestriction.Multiplayer))
			{
				PlatformSystems.userManager.DisplayRestrictionMessage();
				desiredMenuScreen = currentMenuScreen;
				return;
			}
			currentMenuScreen.shouldDisplay = false;
			if (currentMenuScreen.IsReadyToLeave())
			{
				MPEventSystemLocator component = GetComponent<MPEventSystemLocator>();
				if ((bool)component)
				{
					component.eventSystem.SetSelectedObject(null);
				}
				if (currentMenuScreen == multiplayerMenuScreen && desiredMenuScreen == titleMenuScreen)
				{
					MultiplayerMenuController.instance?.ResetLobby();
				}
				currentMenuScreen.OnExit(this);
				currentMenuScreen.gameObject.SetActive(value: false);
				currentMenuScreen = desiredMenuScreen;
				camTransitionTimer = camTransitionDuration;
				currentMenuScreen.OnEnter(this);
				IsOnMultiplayerScreen = currentMenuScreen == multiplayerMenuScreen;
			}
		}
		else if (currentMenuScreen.desiredCameraTransform != null)
		{
			camTransitionTimer -= Time.deltaTime;
			cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, currentMenuScreen.desiredCameraTransform.position, ref camSmoothDampPositionVelocity, camTranslationSmoothDampTime);
			Vector3 eulerAngles = cameraTransform.eulerAngles;
			Vector3 eulerAngles2 = currentMenuScreen.desiredCameraTransform.eulerAngles;
			eulerAngles.x = Mathf.SmoothDampAngle(eulerAngles.x, eulerAngles2.x, ref camSmoothDampRotationVelocity.x, camRotationSmoothDampTime, float.PositiveInfinity, Time.unscaledDeltaTime);
			eulerAngles.y = Mathf.SmoothDampAngle(eulerAngles.y, eulerAngles2.y, ref camSmoothDampRotationVelocity.y, camRotationSmoothDampTime, float.PositiveInfinity, Time.unscaledDeltaTime);
			eulerAngles.z = Mathf.SmoothDampAngle(eulerAngles.z, eulerAngles2.z, ref camSmoothDampRotationVelocity.z, camRotationSmoothDampTime, float.PositiveInfinity, Time.unscaledDeltaTime);
			cameraTransform.eulerAngles = eulerAngles;
			if (camTransitionTimer <= 0f)
			{
				currentMenuScreen.gameObject.SetActive(value: true);
				currentMenuScreen.shouldDisplay = true;
			}
		}
	}

	public void SetAllowTransition(bool value)
	{
		allowTransition = value;
	}

	public void SetDesiredMenuScreen(BaseMainMenuScreen newDesiredMenuScreen)
	{
		if (allowTransition)
		{
			desiredMenuScreen = newDesiredMenuScreen;
		}
	}

	public void ClearEngagementScreen()
	{
		if (!CheckWarningScreen())
		{
			desiredMenuScreen = titleMenuScreen;
		}
	}

	private void UpdateEoSLoginState()
	{
		if (lastEOSLoginState == EOSLoginManager.loginState)
		{
			return;
		}
		switch (EOSLoginManager.loginState)
		{
		case EOSLoginManager.EOSLoginState.AttemptingLink:
			AddAccountLinkPopup();
			break;
		case EOSLoginManager.EOSLoginState.FailedLogin:
			AddAccountLinkPopup();
			break;
		case EOSLoginManager.EOSLoginState.FailedLink:
			AddAccountLinkPopup();
			break;
		case EOSLoginManager.EOSLoginState.Success:
			if (titleMenuScreen.isActiveAndEnabled)
			{
				mainMenuButtonPanel?.SetActive(value: true);
			}
			break;
		}
		lastEOSLoginState = EOSLoginManager.loginState;
	}

	private void AddAccountLinkPopup()
	{
		SimpleDialogBox dialogBox = SimpleDialogBox.Create();
		Action retryLoginFunction = delegate
		{
			if ((bool)dialogBox)
			{
				RetryLogin();
			}
		};
		Action deactiveCrossplayAndRestartFunction = delegate
		{
			if ((bool)dialogBox)
			{
				DeactiveCrossPlayAndRestart();
			}
		};
		dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "EOS_NOT_LINKED_TITLE",
			formatParams = Array.Empty<object>()
		};
		dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "EOS_NOT_LINKED_MESSAGE",
			formatParams = Array.Empty<object>()
		};
		dialogBox.AddActionButton(delegate
		{
			retryLoginFunction();
		}, "EOS_RETRY_LOGIN", true);
		dialogBox.AddActionButton(delegate
		{
			deactiveCrossplayAndRestartFunction();
		}, "EOS_DEACTIVATE_CROSSPLAY_RESTART", true);
	}

	private void RetryLogin()
	{
		if (EOSLoginManager.loggedInAuthId == null)
		{
			new EOSLoginManager().TryLogin();
		}
		else if (titleMenuScreen.isActiveAndEnabled)
		{
			mainMenuButtonPanel?.SetActive(value: true);
		}
	}

	private void DeactiveCrossPlayAndRestart()
	{
		Console.instance.FindConVar("egsToggle")?.AttemptSetString("0");
		Console.instance.SubmitCmd(null, "quit");
	}

	private void ShowFirstTimeCrossPlayPopup()
	{
		SimpleDialogBox dialogBox = SimpleDialogBox.Create();
		Action activateCrossplayFunction = delegate
		{
			if ((bool)dialogBox)
			{
				ActivateCrossPlay();
			}
		};
		Action deactivateCrossplayFunction = delegate
		{
			if ((bool)dialogBox)
			{
				DeactivateCrossPlay();
			}
		};
		Action ShowCrossplayInfo = delegate
		{
			if ((bool)dialogBox)
			{
				Application.OpenURL("https://support.gearboxsoftware.com/hc/en-us/articles/4440999200269");
			}
		};
		dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "FIRST_TIME_CROSSPLAY_POPUP_HEADER",
			formatParams = Array.Empty<object>()
		};
		dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "FIRST_TIME_CROSSPLAY_POPUP_DESCRIPTION",
			formatParams = Array.Empty<object>()
		};
		dialogBox.AddActionButton(delegate
		{
			activateCrossplayFunction();
		}, "STAT_CONTINUE", true);
		dialogBox.AddActionButton(delegate
		{
			ShowCrossplayInfo();
		}, "FIRST_TIME_CROSSPLAY_POPUP_INFO", false);
		dialogBox.AddActionButton(delegate
		{
			deactivateCrossplayFunction();
		}, "FIRST_TIME_CROSSPLAY_POPUP_DISABLE_CROSSPLAY", true);
		static void ActivateCrossPlay()
		{
			PlayerPrefs.SetInt("ShownFirstTimePopup", 1);
		}
		static void DeactivateCrossPlay()
		{
			PlayerPrefs.SetInt("ShownFirstTimePopup", 1);
			Console.instance.FindConVar("egsToggle")?.AttemptSetString("1");
			Console.instance.SubmitCmd(null, "quit");
		}
	}

	public void StartWaitForLoad()
	{
		if (!IsMainUserSignedIn())
		{
			StartCoroutine(WaitForPlatformLoad());
			return;
		}
		MainMenuController.OnPreMainMenuInitialized?.Invoke();
		isInitialized = true;
	}

	private IEnumerator WaitForPlatformLoad()
	{
		MainMenuController.OnPreMainMenuInitialized?.Invoke();
		SaveSystem platformSaveSystem = PlatformSystems.saveSystem;
		platformSaveSystem.LoadInitialData();
		while (!platformSaveSystem.isInitialLoadFinished)
		{
			yield return null;
		}
		PlatformSystems.userManager.EnableUserControllerHandling();
		if (platformSaveSystem.userProfile != null)
		{
			LocalUserManager.SetLocalUsers(new LocalUserManager.LocalUserInitializationInfo[1]
			{
				new LocalUserManager.LocalUserInitializationInfo
				{
					profile = platformSaveSystem.userProfile
				}
			});
			Console.instance.SubmitCmd(null, "exec config");
			logbookButton.interactable = false;
			while (!LogBookController.IsStaticDataReady)
			{
				yield return null;
			}
			logbookButton.interactable = true;
			isInitialized = true;
		}
		else
		{
			isInitialized = true;
		}
	}

	public void EnableContinueEAWarningButton()
	{
		continueButtonTransition.interactable = true;
	}
}
