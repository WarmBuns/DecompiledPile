using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2.UI;

public class SettingsPanelController : MonoBehaviour
{
	[FormerlySerializedAs("carouselControllers")]
	private BaseSettingsControl[] settingsControllers;

	public MPButton revertButton;

	private static string viewableName = "Settings";

	public bool usedForFriendsBrowser;

	public static bool settingsPanelOpen = false;

	public GameObject[] ensureElementsEnabled;

	private bool useEnsure;

	private string confirmControllerDefaultsTitleToken = "OPTION_DEFAULT_SETTINGS";

	private string confirmControllerDefaultsDescriptionToken = "";

	private bool wasRemapping;

	private MPEventSystemLocator eventSystemLocator;

	protected MPEventSystem eventSystem => eventSystemLocator?.eventSystem;

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		ViewablesCatalog.AddNodeToRoot(new ViewablesCatalog.Node(viewableName, isFolder: false)
		{
			shouldShowUnviewed = CheckViewable
		});
	}

	private static bool CheckViewable(UserProfile userProfile)
	{
		return !userProfile.HasViewedViewable("/" + viewableName);
	}

	private void Start()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		settingsControllers = GetComponentsInChildren<BaseSettingsControl>();
	}

	public void ConfirmControllerDefaults()
	{
		SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create(eventSystem);
		simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair(confirmControllerDefaultsTitleToken);
		simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair(confirmControllerDefaultsDescriptionToken);
		simpleDialogBox.AddActionButton(RevertChanges, "DIALOG_OPTION_YES", true);
		simpleDialogBox.AddCancelButton("CANCEL");
	}

	private void UpdateResetToDefaults()
	{
		if (SimpleDialogBox.instancesList.Count < 1 && !wasRemapping && !InputMapperHelper.anyRemapperListening && eventSystem != null && eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad && eventSystem.player.GetButtonDown(31))
		{
			ConfirmControllerDefaults();
		}
		wasRemapping = InputMapperHelper.anyRemapperListening;
	}

	private void Update()
	{
		if (settingsControllers == null)
		{
			return;
		}
		bool interactable = false;
		for (int i = 0; i < settingsControllers.Length; i++)
		{
			BaseSettingsControl baseSettingsControl = settingsControllers[i];
			if ((bool)baseSettingsControl && baseSettingsControl.hasBeenChanged)
			{
				interactable = true;
			}
		}
		if ((bool)revertButton)
		{
			revertButton.interactable = interactable;
		}
		if (!usedForFriendsBrowser)
		{
			UpdateResetToDefaults();
		}
	}

	public void RevertChanges()
	{
		if (base.isActiveAndEnabled)
		{
			BaseSettingsControl[] componentsInChildren = GetComponentsInChildren<BaseSettingsControl>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].ResetToDefault();
			}
			DefaultSettings component = GetComponent<DefaultSettings>();
			if ((bool)component)
			{
				component.RestoreDefaultControllerSettings(eventSystem.player);
			}
		}
	}

	private void OnEnable()
	{
		settingsPanelOpen = true;
		bool active = useEnsure;
		GameObject[] array = ensureElementsEnabled;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(active);
		}
	}

	private void OnDisable()
	{
		if (GetHaveOptionsChanged(forSpecificSource: true))
		{
			Console.instance?.SaveArchiveConVars();
		}
		if ((InputMapperHelper.anyRemapperListening || GetHaveOptionsChanged(forSpecificSource: true, BaseSettingsControl.SettingSource.UserProfilePref)) && settingsControllers != null)
		{
			UserProfile currentUserProfile = settingsControllers[0].GetCurrentUserProfile();
			if (currentUserProfile != null)
			{
				PlatformSystems.saveSystem.RequestSave(currentUserProfile);
			}
		}
		settingsPanelOpen = false;
		GameObject[] array = ensureElementsEnabled;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}

	private bool GetHaveOptionsChanged(bool forSpecificSource = false, BaseSettingsControl.SettingSource source = BaseSettingsControl.SettingSource.ConVar)
	{
		if (settingsControllers == null)
		{
			return false;
		}
		for (int i = 0; i < settingsControllers.Length; i++)
		{
			BaseSettingsControl baseSettingsControl = settingsControllers[i];
			if (baseSettingsControl.hasBeenChanged && (!forSpecificSource || baseSettingsControl.settingSource == source))
			{
				return true;
			}
		}
		return false;
	}
}
