using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class ItemIcon : MonoBehaviour
{
	private InspectPanelLocator inspectPanelLocator;

	private UserProfile userProfile;

	public RawImage glowImage;

	public RawImage image;

	public TextMeshProUGUI stackText;

	public SpriteAsNumberManager spriteAsNumberManager;

	public TooltipProvider tooltipProvider;

	private ItemIndex itemIndex;

	private int itemCount;

	public Action OnItemClicked;

	public RectTransform rectTransform { get; private set; }

	public InspectPanelController inspectPanel
	{
		get
		{
			if ((bool)inspectPanelLocator)
			{
				return inspectPanelLocator.InspectPanel;
			}
			return null;
		}
	}

	private void Awake()
	{
		CacheRectTransform();
		inspectPanelLocator = GetComponent<InspectPanelLocator>();
		userProfile = GetComponentInParent<HUD>().localUserViewer.userProfile;
	}

	public void CacheRectTransform()
	{
		if ((object)rectTransform == null)
		{
			rectTransform = (RectTransform)base.transform;
		}
	}

	public void SetItemIndex(ItemIndex newItemIndex, int newItemCount)
	{
		if (itemIndex == newItemIndex && itemCount == newItemCount)
		{
			return;
		}
		itemIndex = newItemIndex;
		itemCount = newItemCount;
		string titleToken = "";
		string bodyToken = "";
		Color titleColor = Color.white;
		Color bodyColor = new Color(0.6f, 0.6f, 0.6f, 1f);
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		if (itemDef != null)
		{
			image.texture = itemDef.pickupIconTexture;
			if (itemCount > 1)
			{
				spriteAsNumberManager.SetItemCount(itemCount);
			}
			else
			{
				spriteAsNumberManager.SetVisible(b: false);
			}
			titleToken = itemDef.nameToken;
			bodyToken = itemDef.pickupToken;
			titleColor = ColorCatalog.GetColor(itemDef.darkColorIndex);
		}
		if ((bool)glowImage)
		{
			glowImage.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.75f);
		}
		if ((bool)tooltipProvider)
		{
			tooltipProvider.titleToken = titleToken;
			tooltipProvider.bodyToken = bodyToken;
			tooltipProvider.titleColor = titleColor;
			tooltipProvider.bodyColor = bodyColor;
		}
	}

	public void ItemClicked()
	{
		if (!userProfile.useInspectFeature)
		{
			return;
		}
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		if (itemDef != null)
		{
			if ((bool)inspectPanel)
			{
				inspectPanel.Show(itemDef);
			}
			OnItemClicked?.Invoke();
		}
	}
}
