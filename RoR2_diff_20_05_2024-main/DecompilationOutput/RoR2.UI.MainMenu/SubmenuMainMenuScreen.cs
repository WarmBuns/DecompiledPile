using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2.UI.MainMenu;

public class SubmenuMainMenuScreen : BaseMainMenuScreen
{
	[FormerlySerializedAs("settingsPanelPrefab")]
	public GameObject submenuPanelPrefab;

	private GameObject submenuPanelInstance;

	private HGHeaderNavigationController headerNavigationController;

	private new void Awake()
	{
		submenuPanelInstance = Object.Instantiate(submenuPanelPrefab, base.transform);
		submenuPanelInstance.GetComponent<HGHeaderNavigationController>().isPrimaryPlayer = true;
		headerNavigationController = GetComponentInChildren<HGHeaderNavigationController>();
	}

	public override void OnEnter(MainMenuController mainMenuController)
	{
		submenuPanelInstance.SetActive(value: true);
		headerNavigationController?.ChooseFirstHeader();
		base.OnEnter(mainMenuController);
	}

	public override void OnExit(MainMenuController mainMenuController)
	{
		base.OnExit(mainMenuController);
	}

	public new void Update()
	{
		if (!submenuPanelInstance.activeSelf && (bool)myMainMenuController)
		{
			myMainMenuController.SetDesiredMenuScreen(myMainMenuController.titleMenuScreen);
		}
	}
}
