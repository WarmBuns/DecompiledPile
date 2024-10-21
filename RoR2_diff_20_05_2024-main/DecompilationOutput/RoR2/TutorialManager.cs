using RoR2.ConVar;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

[DisallowMultipleComponent]
public class TutorialManager : MonoBehaviour
{
	private static BoolConVar cvIsTutorialEnabled = new BoolConVar("enable_tutorial", ConVarFlags.None, "0", "Enables the tutorial.");

	private LocalUser _cachedLocalUser;

	private Inventory _cachedInventory;

	private float itemTriggerTimeBuffer = 5f;

	public static bool isTutorialEnabled => cvIsTutorialEnabled.value;

	private void Start()
	{
		if (isTutorialEnabled)
		{
			_cachedLocalUser = LocalUserManager.GetFirstLocalUser();
			if (_cachedLocalUser != null)
			{
				_cachedLocalUser.onBodyChanged += OnBodyChanged;
			}
		}
	}

	private void OnBodyChanged()
	{
		_cachedInventory = _cachedLocalUser.cachedMasterController.master.inventory;
		if ((bool)_cachedInventory)
		{
			_cachedLocalUser.onBodyChanged -= OnBodyChanged;
			BindPickups();
		}
	}

	private void BindPickups()
	{
		_cachedInventory.onItemAddedClient += OnItemAddedClient;
		_cachedInventory.onEquipmentChangedClient += OnEquipmentChangedClient;
	}

	private void UnbindPickups()
	{
		_cachedInventory.onItemAddedClient -= OnItemAddedClient;
		_cachedInventory.onEquipmentChangedClient -= OnEquipmentChangedClient;
	}

	private void OnItemAddedClient(ItemIndex itemIndex)
	{
		CameraRigController cameraRigController = _cachedLocalUser.cameraRigController;
		if (!cameraRigController)
		{
			return;
		}
		HUD hud = cameraRigController.hud;
		if (!hud)
		{
			return;
		}
		InspectPanelController inspectPanelController = hud.inspectPanelController;
		if ((bool)inspectPanelController && (!Run.instance || !(Run.instance.GetRunStopwatch() < itemTriggerTimeBuffer)))
		{
			hud.scoreboardPanel.SetActive(value: true);
			if (_cachedLocalUser.userProfile.useInspectFeature)
			{
				InspectInfo inspectInfo = ItemCatalog.GetItemDef(itemIndex);
				inspectInfo.MarkForceShowInfo();
				inspectPanelController.Show(inspectInfo);
			}
			UnbindPickups();
		}
	}

	private void OnEquipmentChangedClient(EquipmentIndex newEquipmentIndex, uint equipmentSlot)
	{
		CameraRigController cameraRigController = _cachedLocalUser.cameraRigController;
		if (!cameraRigController)
		{
			return;
		}
		HUD hud = cameraRigController.hud;
		if (!hud)
		{
			return;
		}
		InspectPanelController inspectPanelController = hud.inspectPanelController;
		if ((bool)inspectPanelController && (!Run.instance || !(Run.instance.GetRunStopwatch() < itemTriggerTimeBuffer)))
		{
			hud.scoreboardPanel.SetActive(value: true);
			if (_cachedLocalUser.userProfile.useInspectFeature)
			{
				InspectInfo inspectInfo = EquipmentCatalog.GetEquipmentDef(newEquipmentIndex);
				inspectInfo.MarkForceShowInfo();
				inspectPanelController.Show(inspectInfo);
			}
			UnbindPickups();
		}
	}
}
