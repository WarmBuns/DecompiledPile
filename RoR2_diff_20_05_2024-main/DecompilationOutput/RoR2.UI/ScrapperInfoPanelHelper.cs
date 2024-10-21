using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class ScrapperInfoPanelHelper : MonoBehaviour
{
	[SerializeField]
	public InspectPanelController inspectPanelController;

	[SerializeField]
	public Image correspondingScrapImage;

	private MPEventSystem eventSystem;

	private Inventory cachedBodyInventory;

	private void Awake()
	{
		MPEventSystemLocator component = GetComponent<MPEventSystemLocator>();
		eventSystem = component.eventSystem;
		if (eventSystem != null && eventSystem.localUser != null && eventSystem.localUser.cachedBody != null)
		{
			cachedBodyInventory = eventSystem.localUser.cachedBody.inventory;
		}
	}

	private void Update()
	{
		if (eventSystem.player.GetButtonDown(15))
		{
			Object.Destroy(base.gameObject);
		}
	}

	public void ShowInfo(MPButton button, PickupDef pickupDef)
	{
		inspectPanelController.Show(pickupDef);
		PickupDef pickupDef2 = PickupCatalog.GetPickupDef(PickupCatalog.FindScrapIndexForItemTier(pickupDef.itemTier));
		if (pickupDef2 != null)
		{
			correspondingScrapImage.sprite = pickupDef2.iconSprite;
		}
	}

	public void AddQuantityToPickerButton(MPButton button, PickupDef pickupDef)
	{
		if (!cachedBodyInventory)
		{
			return;
		}
		int itemCount = cachedBodyInventory.GetItemCount(pickupDef.itemIndex);
		TextMeshProUGUI textMeshProUGUI = button.GetComponent<ChildLocator>().FindChildComponent<TextMeshProUGUI>("Quantity");
		if ((bool)textMeshProUGUI)
		{
			if (itemCount > 1)
			{
				textMeshProUGUI.SetText($"{itemCount}");
			}
			else
			{
				textMeshProUGUI.gameObject.SetActive(value: false);
			}
		}
	}
}
