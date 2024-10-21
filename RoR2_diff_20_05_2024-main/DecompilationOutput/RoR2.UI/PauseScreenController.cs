using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemProvider))]
[RequireComponent(typeof(RectTransform))]
public class PauseScreenController : MonoBehaviour
{
	public static readonly List<PauseScreenController> instancesList = new List<PauseScreenController>();

	[SerializeField]
	private MPEventSystemProvider eventSystemProvider;

	[Tooltip("The main panel to which any submenus will be parented.")]
	public RectTransform rootPanel;

	[Tooltip("The minimal pause panel which will appear during a multiplayer pause.")]
	public RectTransform pausePanel;

	[Tooltip("The panel that contains the blur and controls.")]
	public RectTransform gameMenuPanel;

	[Tooltip("The panel which contains the main controls. This will be disabled while in a submenu.")]
	public RectTransform mainPanel;

	public GameObject settingsPanelPrefab;

	private GameObject submenuObject;

	public GameObject logbookPrefab;

	public GameObject resumeGameplayObject;

	public GameObject exitGameButton;

	public GameObject devCheatMenuPrefab;

	[SerializeField]
	private GameObject pauseButtonObject;

	private HGButton pauseButton;

	[SerializeField]
	private GameObject inventoryButtonObject;

	[SerializeField]
	private LanguageTextMeshController pauseLabel;

	[SerializeField]
	private HGButton resumeButton;

	[SerializeField]
	private float resumeButtonTimer = 1f;

	private bool _shouldResumeOnDisable = true;

	public LocalUser localUser;

	private static bool paused = false;

	public static bool pauseScreenUp = false;

	public static int storedHUDSize = -1;

	public bool isMinimallyPause => pausePanel.gameObject.activeSelf;

	private void Awake()
	{
		UpdateEventSystemProvider();
		pauseButton = pauseButtonObject.GetComponent<HGButton>();
		if (!NetworkServer.dontListen && (bool)PauseStopController.instance && PauseStopController.instance.allowMultiplayerPause)
		{
			pauseButtonObject.SetActive(value: true);
		}
		if ((bool)PauseStopController.instance && PauseStopController.instance.isPaused)
		{
			pauseButton.interactable = false;
		}
	}

	public void UpdateEventSystemProvider()
	{
		MPEventSystemProvider component = GetComponent<MPEventSystemProvider>();
		component.fallBackToMainEventSystem = false;
		component.fallBackToPlayer2EventSystem = false;
		component.fallBackToPlayer3EventSystem = false;
		component.fallBackToPlayer4EventSystem = false;
		switch (PauseManager.PausingPlayerID)
		{
		case 0:
			component.fallBackToMainEventSystem = true;
			localUser = LocalUserManager.FindLocalUserByPlayerName("PlayerMain");
			break;
		case 1:
			component.fallBackToPlayer2EventSystem = true;
			localUser = LocalUserManager.FindLocalUserByPlayerName("Player2");
			break;
		case 2:
			component.fallBackToPlayer3EventSystem = true;
			localUser = LocalUserManager.FindLocalUserByPlayerName("Player3");
			break;
		case 3:
			component.fallBackToPlayer4EventSystem = true;
			localUser = LocalUserManager.FindLocalUserByPlayerName("Player4");
			break;
		default:
			component.fallBackToMainEventSystem = true;
			localUser = LocalUserManager.FindLocalUserByPlayerName("PlayerMain");
			break;
		}
		localUser.pauseScreenController = this;
	}

	public void GameplayResumed()
	{
		resumeGameplayObject.SetActive(value: true);
	}

	private void Start()
	{
		if (!PauseStopController.instance || !PauseStopController.instance.canControlPause)
		{
			pauseButton.interactable = false;
		}
	}

	public static GameObject InstantiatePauseScreen(bool shouldPauseStop)
	{
		if (instancesList.Count != 0)
		{
			return null;
		}
		GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/PauseScreen"), RoR2Application.instance.transform);
		PauseScreenController component = obj.GetComponent<PauseScreenController>();
		instancesList.Add(component);
		HUD.SetScoreboardActive(component.localUser, isActive: false, EScoreboardDeactivateReturnContext.Game);
		if (NetworkServer.dontListen)
		{
			PauseStopController instance = PauseStopController.instance;
			if ((object)instance == null)
			{
				return obj;
			}
			instance.Pause(shouldPauseStop);
		}
		return obj;
	}

	public static GameObject InstantiateMinimalPauseScreen()
	{
		if (instancesList.Count != 0)
		{
			return null;
		}
		GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/PauseScreen"), RoR2Application.instance.transform);
		PauseScreenController component = obj.GetComponent<PauseScreenController>();
		component.pausePanel.gameObject.SetActive(value: true);
		component.gameMenuPanel.gameObject.SetActive(value: false);
		HUD.SetScoreboardActive(component.localUser, isActive: false, EScoreboardDeactivateReturnContext.Game);
		instancesList.Add(component);
		return obj;
	}

	public void DestroyPauseScreen(bool shouldResumeOnDisable)
	{
		_shouldResumeOnDisable = shouldResumeOnDisable;
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OpenGameMenu()
	{
		if (isMinimallyPause)
		{
			pausePanel.gameObject.SetActive(value: false);
			gameMenuPanel.gameObject.SetActive(value: true);
		}
	}

	private void OnEnable()
	{
		if (instancesList.Count == 0)
		{
			pauseScreenUp = true;
			UserProfile userProfile = NetworkUser.readOnlyLocalPlayersList[0]?.localUser?.userProfile;
			if (userProfile != null)
			{
				storedHUDSize = userProfile.hudSizeMode;
			}
		}
		if (!Run.instance || ((bool)Run.instance && !Run.instance.isRunning))
		{
			inventoryButtonObject.SetActive(value: false);
		}
		PauseManager.onPauseStartGlobal = (Action)Delegate.Combine(PauseManager.onPauseStartGlobal, new Action(OnPauseStart));
		PauseManager.onPauseEndGlobal = (Action)Delegate.Combine(PauseManager.onPauseEndGlobal, new Action(OnPauseEnd));
		PauseManager.isPaused = true;
	}

	private void OnDisable()
	{
		PauseManager.onPauseStartGlobal = (Action)Delegate.Remove(PauseManager.onPauseStartGlobal, new Action(OnPauseStart));
		PauseManager.onPauseEndGlobal = (Action)Delegate.Remove(PauseManager.onPauseEndGlobal, new Action(OnPauseEnd));
		if (localUser != null && localUser.pauseScreenController == this)
		{
			localUser.pauseScreenController = null;
		}
		instancesList.Remove(this);
		if (instancesList.Count == 0)
		{
			UpdateHudIfNecessary();
			if (!_shouldResumeOnDisable)
			{
				_shouldResumeOnDisable = true;
				return;
			}
			PauseManager.isPaused = false;
			PauseStopController.instance?.Pause(shouldPause: false);
		}
	}

	private void UpdateHudIfNecessary()
	{
		UserProfile userProfile = null;
		if (NetworkUser.readOnlyLocalPlayersList != null && NetworkUser.readOnlyLocalPlayersList.Count > 0)
		{
			userProfile = NetworkUser.readOnlyLocalPlayersList[0]?.localUser?.userProfile;
		}
		if (userProfile != null && userProfile.hudSizeMode != storedHUDSize)
		{
			CameraRigController.OnChangeHUD?.Invoke();
			storedHUDSize = userProfile.hudSizeMode;
		}
	}

	private void OnPauseStart()
	{
		pauseButton.interactable = false;
		pauseLabel.token = "PAUSE_PAUSED";
		if (!NetworkServer.dontListen)
		{
			resumeButton.interactable = false;
			StartCoroutine(EnableResumeButton(resumeButtonTimer));
		}
		resumeGameplayObject.SetActive(value: false);
	}

	private void OnPauseEnd()
	{
		if ((bool)PauseStopController.instance && PauseStopController.instance.canControlPause)
		{
			pauseButton.interactable = true;
			pauseScreenUp = false;
			if (paused)
			{
				paused = false;
				if (PauseManager.onPauseEndGlobal != null)
				{
					PauseManager.onPauseEndGlobal();
				}
			}
			UpdateHudIfNecessary();
		}
		resumeButton.interactable = true;
		pauseLabel.token = "PAUSE_PAUSE";
	}

	public void OpenSettingsMenu()
	{
		UnityEngine.Object.Destroy(submenuObject);
		submenuObject = UnityEngine.Object.Instantiate(settingsPanelPrefab, rootPanel);
		if (eventSystemProvider != null)
		{
			submenuObject.GetComponent<HGHeaderNavigationController>().isPrimaryPlayer = eventSystemProvider.fallBackToMainEventSystem;
		}
		pausePanel.gameObject.SetActive(value: false);
		gameMenuPanel.gameObject.SetActive(value: true);
		mainPanel.gameObject.SetActive(value: false);
	}

	public void OpenDevCheatMenu()
	{
		UnityEngine.Object.Destroy(submenuObject);
		submenuObject = UnityEngine.Object.Instantiate(devCheatMenuPrefab, rootPanel);
		mainPanel.gameObject.SetActive(value: false);
	}

	public void OpenScoreboard()
	{
		if (HUD.SetScoreboardActive(localUser, isActive: true, EScoreboardDeactivateReturnContext.Pause, shouldForceScoreboardToggle: true))
		{
			PauseManager.ClosePauseScreen(shouldResume: false);
		}
	}

	public void OpenLogbook()
	{
		UnityEngine.Object.Destroy(submenuObject);
		submenuObject = UnityEngine.Object.Instantiate(logbookPrefab, rootPanel);
		if (submenuObject.transform is RectTransform rectTransform)
		{
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.localScale = new Vector3(1f, 1f, 1f);
		}
		pausePanel.gameObject.SetActive(value: false);
		gameMenuPanel.gameObject.SetActive(value: true);
		mainPanel.gameObject.SetActive(value: false);
	}

	public void ServerPauseGameButton()
	{
		PauseStopController.instance?.Pause(shouldPause: true);
	}

	public void Update()
	{
		if (!submenuObject && !isMinimallyPause)
		{
			mainPanel.gameObject.SetActive(value: true);
		}
		if (!NetworkManager.singleton.isNetworkActive)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private IEnumerator EnableResumeButton(float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		resumeButton.interactable = true;
	}
}
