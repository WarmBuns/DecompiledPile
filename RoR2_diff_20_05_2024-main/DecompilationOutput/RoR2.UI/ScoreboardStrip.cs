using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class ScoreboardStrip : MonoBehaviour
{
	public ItemInventoryDisplay itemInventoryDisplay;

	public Image nameFocusHighlight;

	public EquipmentIcon equipmentIcon;

	public SocialUserIcon userAvatar;

	public TextMeshProUGUI nameLabel;

	public RawImage classIcon;

	public TextMeshProUGUI moneyText;

	public TextMeshProUGUI itemCountText;

	public MPEventSystemLocator eventSystemLocator;

	private CharacterMaster master;

	private CharacterBody userBody;

	private PlayerCharacterMasterController userPlayerCharacterMasterController;

	private uint previousMoney = uint.MaxValue;

	private int previousItemCount = int.MaxValue;

	public void SetMaster(CharacterMaster newMaster)
	{
		userBody = null;
		master = newMaster;
		if ((bool)master)
		{
			userBody = master.GetBody();
			userPlayerCharacterMasterController = master.GetComponent<PlayerCharacterMasterController>();
			itemInventoryDisplay.SetSubscribedInventory(master.inventory);
			equipmentIcon.targetInventory = master.inventory;
			UpdateMoneyText();
			UpdateItemCountText();
		}
		if ((bool)userAvatar && userAvatar.isActiveAndEnabled)
		{
			userAvatar.SetFromMaster(newMaster);
		}
		nameLabel.text = Util.GetBestMasterName(master);
		classIcon.texture = FindMasterPortrait();
	}

	private void UpdateMoneyText()
	{
		if ((bool)master && master.money != previousMoney)
		{
			previousMoney = master.money;
			moneyText.text = $"${previousMoney}";
		}
	}

	private void UpdateItemCountText()
	{
		int totalVisibleItemCount = itemInventoryDisplay.GetTotalVisibleItemCount();
		if ((bool)master && (bool)itemInventoryDisplay && totalVisibleItemCount != previousItemCount)
		{
			previousItemCount = totalVisibleItemCount;
			itemCountText.text = previousItemCount.ToString();
		}
	}

	private void Update()
	{
		UpdateMoneyText();
		UpdateItemCountText();
	}

	private Texture FindMasterPortrait()
	{
		if ((bool)userBody)
		{
			return userBody.portraitIcon;
		}
		if ((bool)master)
		{
			GameObject bodyPrefab = master.bodyPrefab;
			if ((bool)bodyPrefab)
			{
				CharacterBody component = bodyPrefab.GetComponent<CharacterBody>();
				if ((bool)component)
				{
					return component.portraitIcon;
				}
			}
		}
		return null;
	}

	public void SelectFirstItem()
	{
		if (!itemInventoryDisplay.SelectFirstItemIcon())
		{
			equipmentIcon.TrySelect();
		}
	}

	public void EnterStrip()
	{
		SelectFirstItem();
	}

	public void GetPlayerCard()
	{
	}

	public void OnNavItemFocused(Image image)
	{
		image.color = Color.yellow;
	}

	public void NavItemUnfocused(Image image)
	{
		image.color = Color.black;
	}

	private void ResetNavIndicators()
	{
		itemInventoryDisplay.GetComponent<Image>().color = Color.black;
		nameFocusHighlight.color = Color.black;
	}

	private void OnDisable()
	{
		ResetNavIndicators();
	}
}
