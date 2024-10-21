using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoR2.UI.MainMenu;

[RequireComponent(typeof(MPEventSystemLocator))]
public class ProfileMainMenuScreen : BaseMainMenuScreen
{
	public GameObject createProfilePanel;

	public TMP_InputField createProfileNameInputField;

	public MPButton submitProfileNameButton;

	public GameObject gotoSelectProfilePanelButtonContainer;

	public GameObject selectProfilePanel;

	public MPButton gotoCreateProfilePanelButton;

	public UserProfileListController existingProfileListController;

	private MPEventSystemLocator eventSystemLocator;

	private bool firstTimeConfiguration;

	private const string defaultName = "Nameless Survivor";

	private string GuessDefaultProfileName()
	{
		return PlatformSystems.saveSystem.GetPlatformUsernameOrDefault("Nameless Survivor");
	}

	public UserProfile Editor_TryOpenTestProfile()
	{
		List<string> availableProfileNames = PlatformSystems.saveSystem.GetAvailableProfileNames();
		if (availableProfileNames.Count > 0)
		{
			Debug.LogWarning("Got existing profile!");
			return PlatformSystems.saveSystem.GetProfile(availableProfileNames[0]);
		}
		return null;
	}

	public void OpenDefaultProfile()
	{
		GuessDefaultProfileName();
		UserProfile mainProfile = null;
		SetMainProfile(mainProfile);
	}

	protected new void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		existingProfileListController.onProfileSelected += SetMainProfile;
		existingProfileListController.onListRebuilt += OnListRebuilt;
	}

	protected new void OnEnable()
	{
		firstTimeConfiguration = true;
		List<string> availableProfileNames = PlatformSystems.saveSystem.GetAvailableProfileNames();
		for (int i = 0; i < availableProfileNames.Count; i++)
		{
			if (IsProfileCustom(PlatformSystems.saveSystem.GetProfile(availableProfileNames[i])))
			{
				firstTimeConfiguration = false;
				break;
			}
		}
		if (firstTimeConfiguration)
		{
			OpenCreateProfileMenu(firstTime: true);
			return;
		}
		createProfilePanel.SetActive(value: false);
		selectProfilePanel.SetActive(value: true);
		OnListRebuilt();
		gotoSelectProfilePanelButtonContainer.SetActive(value: true);
	}

	public void OpenCreateProfileMenu(bool firstTime)
	{
		selectProfilePanel.SetActive(value: false);
		createProfilePanel.SetActive(value: true);
		createProfileNameInputField.text = GuessDefaultProfileName();
		if (firstTime)
		{
			gotoSelectProfilePanelButtonContainer.SetActive(value: false);
		}
	}

	private void OnListRebuilt()
	{
		existingProfileListController.GetReadOnlyElementsList();
	}

	protected void OnDisable()
	{
	}

	private void SetMainProfile(UserProfile profile)
	{
		LocalUserManager.SetLocalUsers(new LocalUserManager.LocalUserInitializationInfo[1]
		{
			new LocalUserManager.LocalUserInitializationInfo
			{
				profile = profile
			}
		});
		if (myMainMenuController != null)
		{
			myMainMenuController.desiredMenuScreen = myMainMenuController.titleMenuScreen;
		}
		else
		{
			Debug.LogError("myMainMenuController reference null on ProfileMainMenuScreen.cs while trying to run SetMainProfile(UserProfile profile)");
		}
	}

	private static bool IsProfileCustom(UserProfile profile)
	{
		return profile.fileName != "default";
	}

	private static bool IsNewProfileNameAcceptable(string newProfileName)
	{
		if (PlatformSystems.saveSystem.GetProfile(newProfileName) != null)
		{
			return false;
		}
		if (newProfileName == "")
		{
			return false;
		}
		return true;
	}

	public void OnAddProfilePressed()
	{
		if (eventSystemLocator.eventSystem.currentSelectedGameObject == createProfileNameInputField.gameObject && !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			return;
		}
		string text = createProfileNameInputField.text;
		if (IsNewProfileNameAcceptable(text))
		{
			createProfileNameInputField.text = "";
			UserProfile userProfile = PlatformSystems.saveSystem.CreateProfile(RoR2Application.cloudStorage, text, 0uL);
			if (userProfile != null)
			{
				SetMainProfile(userProfile);
			}
		}
	}

	protected new void Update()
	{
		if (!eventSystemLocator.eventSystem || eventSystemLocator.eventSystem.player == null || !eventSystemLocator.eventSystem.player.GetButton(31))
		{
			return;
		}
		GameObject currentSelectedGameObject = MPEventSystemManager.combinedEventSystem.currentSelectedGameObject;
		if (!currentSelectedGameObject)
		{
			return;
		}
		UserProfileListElementController component = currentSelectedGameObject.GetComponent<UserProfileListElementController>();
		if ((bool)component)
		{
			if (component.userProfile == null)
			{
				Debug.LogError("!!!???");
				return;
			}
			SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
			string consoleString = "user_profile_delete \"" + component.userProfile.fileName + "\"";
			simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
			{
				token = "USER_PROFILE_DELETE_HEADER",
				formatParams = null
			};
			simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
			{
				token = "USER_PROFILE_DELETE_DESCRIPTION",
				formatParams = new object[1] { component.userProfile.name }
			};
			simpleDialogBox.AddCommandButton(consoleString, "USER_PROFILE_DELETE_YES");
			simpleDialogBox.AddCancelButton("USER_PROFILE_DELETE_NO");
		}
	}
}
