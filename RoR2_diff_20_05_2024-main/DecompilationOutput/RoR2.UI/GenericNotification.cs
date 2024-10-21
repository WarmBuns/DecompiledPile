using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class GenericNotification : MonoBehaviour
{
	public LanguageTextMeshController titleText;

	public TextMeshProUGUI titleTMP;

	public LanguageTextMeshController descriptionText;

	public RawImage iconImage;

	public RawImage previousIconImage;

	public CanvasGroup canvasGroup;

	public float fadeOutT = 0.916f;

	public void SetNotificationT(float t)
	{
		canvasGroup.alpha = 1f - Mathf.Clamp01(t - fadeOutT) / (1f - fadeOutT);
	}

	public void SetItem(ItemDef itemDef)
	{
		titleText.token = itemDef.nameToken;
		descriptionText.token = itemDef.pickupToken;
		if (itemDef.pickupIconTexture != null)
		{
			iconImage.texture = itemDef.pickupIconTexture;
		}
		titleTMP.color = ColorCatalog.GetColor(itemDef.colorIndex);
	}

	public void SetEquipment(EquipmentDef equipmentDef)
	{
		titleText.token = equipmentDef.nameToken;
		descriptionText.token = equipmentDef.pickupToken;
		if ((bool)equipmentDef.pickupIconTexture)
		{
			iconImage.texture = equipmentDef.pickupIconTexture;
		}
		titleTMP.color = ColorCatalog.GetColor(equipmentDef.colorIndex);
	}

	public void SetArtifact(ArtifactDef artifactDef)
	{
		titleText.token = artifactDef.nameToken;
		descriptionText.token = artifactDef.descriptionToken;
		iconImage.texture = artifactDef.smallIconSelectedSprite.texture;
		titleTMP.color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Artifact);
	}

	public void SetPreviousItem(ItemDef itemDef)
	{
		if ((bool)previousIconImage && (bool)itemDef.pickupIconTexture)
		{
			previousIconImage.texture = itemDef.pickupIconTexture;
		}
	}

	public void SetPreviousEquipment(EquipmentDef equipmentDef)
	{
		if ((bool)previousIconImage && (bool)equipmentDef.pickupIconTexture)
		{
			previousIconImage.texture = equipmentDef.pickupIconTexture;
		}
	}
}
