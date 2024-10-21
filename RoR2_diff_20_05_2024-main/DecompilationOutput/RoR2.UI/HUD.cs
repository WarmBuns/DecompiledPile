using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(MPEventSystemProvider))]
public class HUD : MonoBehaviour
{
	public delegate void ShouldHudDisplayDelegate(HUD hud, ref bool shouldDisplay);

	private static List<HUD> instancesList;

	private static bool lockInstancesList;

	private static List<GameObject> instancesToReenableList;

	public static readonly ReadOnlyCollection<HUD> readOnlyInstanceList;

	private bool shouldForceScoreboardToggle;

	public GameObject oldTarget;

	private LocalUser _localUserViewer;

	[Header("Main")]
	[Tooltip("Immediate child of this object which contains all other UI.")]
	public GameObject mainContainer;

	private Canvas _mainContainerCanvas;

	[NonSerialized]
	public CameraRigController cameraRigController;

	public GameObject scoreboardPanel;

	public InspectPanelController inspectPanelController;

	public GameObject mainUIPanel;

	public GameObject cinematicPanel;

	public CombatHealthBarViewer combatHealthBarViewer;

	public ContextManager contextManager;

	public AllyCardManager allyCardManager;

	public Transform gameModeUiRoot;

	[Header("Character")]
	public HealthBar healthBar;

	public ExpBar expBar;

	public LevelText levelText;

	public BuffDisplay buffDisplay;

	public MoneyText moneyText;

	public GameObject lunarCoinContainer;

	public MoneyText lunarCoinText;

	public GameObject voidCoinContainer;

	public MoneyText voidCoinText;

	public SkillIcon[] skillIcons;

	public EquipmentIcon[] equipmentIcons;

	public ItemInventoryDisplay itemInventoryDisplay;

	[Header("Debug")]
	public HUDSpeedometer speedometer;

	private MPEventSystemProvider eventSystemProvider;

	private Canvas canvas;

	private EScoreboardDeactivateReturnContext scoreboardDeactivateReturnContext;

	private GameObject previousTargetBodyObject;

	public static readonly BoolConVar cvHudEnable;

	public GameObject targetBodyObject
	{
		get
		{
			if (!targetMaster)
			{
				return null;
			}
			return targetMaster.GetBodyObject();
		}
	}

	public CharacterMaster targetMaster { get; set; }

	public LocalUser localUserViewer
	{
		get
		{
			return _localUserViewer;
		}
		set
		{
			if (_localUserViewer != value)
			{
				_localUserViewer = value;
				eventSystemProvider.eventSystem = _localUserViewer.eventSystem;
			}
		}
	}

	public Canvas mainContainerCanvas
	{
		get
		{
			if (_mainContainerCanvas == null)
			{
				_mainContainerCanvas = mainContainer.GetComponent<Canvas>();
			}
			return _mainContainerCanvas;
		}
		set
		{
		}
	}

	public PostProcessVolume scoreboardPostProcessVolume { get; private set; }

	public GameObject gameModeUiInstance { get; private set; }

	public static event Action<HUD> onHudTargetChangedGlobal;

	public static event ShouldHudDisplayDelegate shouldHudDisplay;

	static HUD()
	{
		instancesList = new List<HUD>();
		lockInstancesList = false;
		instancesToReenableList = new List<GameObject>();
		readOnlyInstanceList = instancesList.AsReadOnly();
		cvHudEnable = new BoolConVar("hud_enable", ConVarFlags.Archive, "1", "Enable/disable the HUD.");
	}

	private static void OnUICameraPreRender(UICamera uiCamera)
	{
		CameraRigController cameraRigController = uiCamera.cameraRigController;
		if (!cameraRigController)
		{
			return;
		}
		LocalUser localUser = (cameraRigController.viewer ? cameraRigController.viewer.localUser : null);
		if (localUser == null)
		{
			return;
		}
		lockInstancesList = true;
		for (int i = 0; i < instancesList.Count; i++)
		{
			HUD hUD = instancesList[i];
			if (hUD.localUserViewer == localUser)
			{
				hUD.canvas.worldCamera = uiCamera.camera;
				continue;
			}
			GameObject gameObject = hUD.gameObject;
			instancesToReenableList.Add(gameObject);
			gameObject.SetActive(value: false);
		}
		lockInstancesList = false;
	}

	private static void OnUICameraPostRender(UICamera uiCamera)
	{
		lockInstancesList = true;
		for (int i = 0; i < instancesToReenableList.Count; i++)
		{
			instancesToReenableList[i].SetActive(value: true);
		}
		instancesToReenableList.Clear();
		lockInstancesList = false;
	}

	public static bool SetScoreboardActive(LocalUser localUser, bool isActive, EScoreboardDeactivateReturnContext returnContext, bool shouldForceScoreboardToggle = false)
	{
		if (localUser == null)
		{
			return false;
		}
		CameraRigController cameraRigController = localUser.cameraRigController;
		if (!cameraRigController)
		{
			return false;
		}
		HUD hud = cameraRigController.hud;
		hud.SetScoreboardState(isActive);
		if (isActive)
		{
			hud.shouldForceScoreboardToggle = shouldForceScoreboardToggle;
			hud.scoreboardDeactivateReturnContext = returnContext;
		}
		return true;
	}

	public static bool IsScoreboardActive(LocalUser localUser)
	{
		if (localUser == null)
		{
			return false;
		}
		CameraRigController cameraRigController = localUser.cameraRigController;
		if (!cameraRigController)
		{
			return false;
		}
		return cameraRigController.hud.scoreboardPanel.activeSelf;
	}

	public void OnEnable()
	{
		if (!lockInstancesList)
		{
			instancesList.Add(this);
			UpdateHudVisibility();
		}
	}

	public void OnDisable()
	{
		if (!lockInstancesList)
		{
			instancesList.Remove(this);
		}
	}

	private void Awake()
	{
		eventSystemProvider = GetComponent<MPEventSystemProvider>();
		canvas = GetComponent<Canvas>();
		if ((bool)scoreboardPanel)
		{
			scoreboardPostProcessVolume = scoreboardPanel.GetComponentInChildren<PostProcessVolume>();
			scoreboardPanel.SetActive(value: false);
		}
		mainUIPanel.SetActive(value: false);
		if ((bool)Run.instance)
		{
			gameModeUiInstance = Run.instance.InstantiateUi(gameModeUiRoot);
		}
	}

	private void OnDestroy()
	{
		if ((bool)Run.instance)
		{
			Run.instance.UnregisterUi(gameModeUiInstance);
		}
	}

	public void Update()
	{
		NetworkUser networkUser = ((!targetMaster) ? null : targetMaster.GetComponent<PlayerCharacterMasterController>()?.networkUser);
		PlayerCharacterMasterController playerCharacterMasterController = (targetMaster ? targetMaster.GetComponent<PlayerCharacterMasterController>() : null);
		Inventory inventory = (targetMaster ? targetMaster.inventory : null);
		GameObject gameObject = targetBodyObject;
		CharacterBody characterBody = (gameObject ? gameObject.GetComponent<CharacterBody>() : null);
		if ((bool)healthBar && (bool)targetBodyObject && gameObject != oldTarget)
		{
			healthBar.source = targetBodyObject.GetComponent<HealthComponent>();
			healthBar.InitializeHealthBar();
		}
		oldTarget = gameObject;
		if ((bool)expBar)
		{
			expBar.source = targetMaster;
		}
		if ((bool)levelText)
		{
			levelText.source = characterBody;
		}
		if ((bool)moneyText)
		{
			moneyText.targetValue = (int)(targetMaster ? targetMaster.money : 0);
		}
		if ((bool)lunarCoinContainer)
		{
			bool flag = localUserViewer != null && localUserViewer.userProfile.totalCollectedCoins != 0;
			uint targetValue = (networkUser ? networkUser.lunarCoins : 0u);
			lunarCoinContainer.SetActive(flag);
			if (flag && (bool)lunarCoinText)
			{
				lunarCoinText.targetValue = (int)targetValue;
			}
		}
		if ((bool)voidCoinContainer)
		{
			int num = (int)(targetMaster ? targetMaster.voidCoins : 0);
			bool flag2 = num > 0;
			voidCoinContainer.SetActive(flag2);
			if (flag2 && (bool)voidCoinText)
			{
				voidCoinText.targetValue = num;
			}
		}
		if ((bool)itemInventoryDisplay)
		{
			itemInventoryDisplay.SetSubscribedInventory(inventory);
		}
		if ((bool)targetBodyObject)
		{
			SkillLocator component = targetBodyObject.GetComponent<SkillLocator>();
			if ((bool)component)
			{
				if (skillIcons.Length != 0 && (bool)skillIcons[0])
				{
					skillIcons[0].targetSkillSlot = SkillSlot.Primary;
					skillIcons[0].targetSkill = component.primary;
					skillIcons[0].playerCharacterMasterController = playerCharacterMasterController;
				}
				if (skillIcons.Length > 1 && (bool)skillIcons[1])
				{
					skillIcons[1].targetSkillSlot = SkillSlot.Secondary;
					skillIcons[1].targetSkill = component.secondary;
					skillIcons[1].playerCharacterMasterController = playerCharacterMasterController;
				}
				if (skillIcons.Length > 2 && (bool)skillIcons[2])
				{
					skillIcons[2].targetSkillSlot = SkillSlot.Utility;
					skillIcons[2].targetSkill = component.utility;
					skillIcons[2].playerCharacterMasterController = playerCharacterMasterController;
				}
				if (skillIcons.Length > 3 && (bool)skillIcons[3])
				{
					skillIcons[3].targetSkillSlot = SkillSlot.Special;
					skillIcons[3].targetSkill = component.special;
					skillIcons[3].playerCharacterMasterController = playerCharacterMasterController;
				}
			}
		}
		EquipmentIcon[] array = equipmentIcons;
		foreach (EquipmentIcon obj in array)
		{
			obj.targetInventory = inventory;
			obj.targetEquipmentSlot = (targetBodyObject ? targetBodyObject.GetComponent<EquipmentSlot>() : null);
			obj.playerCharacterMasterController = (targetMaster ? targetMaster.GetComponent<PlayerCharacterMasterController>() : null);
		}
		if ((bool)buffDisplay)
		{
			buffDisplay.SetSource(characterBody);
		}
		if ((bool)allyCardManager)
		{
			allyCardManager.sourceGameObject = targetBodyObject;
		}
		if ((bool)scoreboardPanel)
		{
			bool flag3 = localUserViewer.userProfile.useToggleScoreboard || shouldForceScoreboardToggle;
			bool flag4 = localUserViewer != null && localUserViewer.inputPlayer != null && localUserViewer.inputPlayer.GetButton("info");
			if (((flag4 && localUserViewer.inputPlayer.GetButtonDown("info")) || !flag3) && (bool)targetBodyObject)
			{
				bool activeSelf = scoreboardPanel.activeSelf;
				bool flag5 = (flag3 ? (!activeSelf) : flag4);
				if (flag5 != activeSelf && !PauseManager.PauseScreenIsOpen())
				{
					SetScoreboardState(flag5);
				}
			}
		}
		if ((bool)speedometer)
		{
			speedometer.targetTransform = (targetBodyObject ? targetBodyObject.transform : null);
		}
		if ((bool)combatHealthBarViewer)
		{
			combatHealthBarViewer.viewerBody = characterBody;
			combatHealthBarViewer.viewerBodyObject = targetBodyObject;
			combatHealthBarViewer.viewerTeamIndex = TeamComponent.GetObjectTeam(targetBodyObject);
		}
		if ((object)targetBodyObject != previousTargetBodyObject)
		{
			previousTargetBodyObject = targetBodyObject;
			HUD.onHudTargetChangedGlobal?.Invoke(this);
		}
		int j = 0;
		for (int num2 = base.transform.childCount - 1; j < num2; j++)
		{
			base.transform.GetChild(j).gameObject.SetActive(value: false);
		}
		if (base.transform.childCount > 0)
		{
			base.transform.GetChild(base.transform.childCount - 1).gameObject.SetActive(value: true);
		}
		UpdateHudVisibility();
	}

	public void SetScoreboardState(bool newState)
	{
		if (newState != scoreboardPanel.activeSelf)
		{
			if (newState)
			{
				ActivateScoreboard();
			}
			else
			{
				DeactivateScoreboard();
			}
		}
	}

	public void ActivateScoreboard()
	{
		PauseManager.ClosePauseScreen(shouldResume: false);
		scoreboardPanel.SetActive(value: true);
		scoreboardPanel.GetComponent<ScoreboardController>()?.SelectFirstScoreboardStrip();
		if (!localUserViewer.userProfile.useInspectFeature)
		{
			return;
		}
		InteractionDriver component = targetBodyObject.GetComponent<InteractionDriver>();
		if (!(component.currentInteractable != null))
		{
			return;
		}
		IInspectable component2 = component.currentInteractable.GetComponent<IInspectable>();
		if (component2 != null)
		{
			IInspectInfoProvider inspectInfoProvider = component2.GetInspectInfoProvider();
			if (inspectInfoProvider != null && inspectInfoProvider.CanBeInspected())
			{
				inspectPanelController.Show(component2.GetInspectInfoProvider().GetInfo(), WithSidecar: true);
			}
		}
	}

	public void DeactivateScoreboard()
	{
		scoreboardPanel.SetActive(value: false);
		if (RoR2Application.isInSinglePlayer)
		{
			EScoreboardDeactivateReturnContext eScoreboardDeactivateReturnContext = scoreboardDeactivateReturnContext;
			if (eScoreboardDeactivateReturnContext != 0 && eScoreboardDeactivateReturnContext == EScoreboardDeactivateReturnContext.Pause)
			{
				PauseManager.TogglePauseScreen();
			}
			else
			{
				PauseStopController.instance?.Pause(shouldPause: false);
			}
		}
		else if ((bool)PauseStopController.instance && PauseStopController.instance.isPaused)
		{
			switch (scoreboardDeactivateReturnContext)
			{
			case EScoreboardDeactivateReturnContext.Pause:
				PauseManager.TogglePauseScreen();
				break;
			case EScoreboardDeactivateReturnContext.Game:
				PauseManager.OpenMinimalPauseScreen();
				break;
			}
		}
		scoreboardDeactivateReturnContext = EScoreboardDeactivateReturnContext.Game;
		shouldForceScoreboardToggle = false;
	}

	private void UpdateHudVisibility()
	{
		if (mainContainer.activeInHierarchy)
		{
			bool shouldDisplay = cameraRigController?.isHudAllowed ?? false;
			HUD.shouldHudDisplay?.Invoke(this, ref shouldDisplay);
			bool flag = cvHudEnable.value && shouldDisplay;
			if (mainUIPanel.activeSelf != flag)
			{
				mainUIPanel.SetActive(flag);
			}
		}
	}
}
