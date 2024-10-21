using UnityEngine;

namespace RoR2.UI;

public class NotificationUIController : MonoBehaviour
{
	[SerializeField]
	private HUD hud;

	[SerializeField]
	private GameObject genericNotificationPrefab;

	[SerializeField]
	private GameObject genericTransformationNotificationPrefab;

	[SerializeField]
	private GameObject contagiousVoidTransformationNotificationPrefab;

	[SerializeField]
	private GameObject suppressedTransformationNotificationPrefab;

	[SerializeField]
	private GameObject cloverVoidTransformationNotificationPrefab;

	[SerializeField]
	private GameObject lunarSunTransformationNotificationPrefab;

	[SerializeField]
	private GameObject regeneratingScrapRegenTransformationNotificationPrefab;

	[SerializeField]
	private GameObject LowerPricedChestsRegenTransformationNotificationPrefab;

	[SerializeField]
	private GameObject teleportOnLowHealthRegenTransformationNotificationPrefab;

	private GenericNotification currentNotification;

	private CharacterMasterNotificationQueue notificationQueue;

	private CharacterMaster targetMaster;

	public void OnEnable()
	{
		CharacterMaster.onCharacterMasterLost += OnCharacterMasterLost;
	}

	public void OnDisable()
	{
		CharacterMaster.onCharacterMasterLost -= OnCharacterMasterLost;
		CleanUpCurrentMaster();
	}

	public void Update()
	{
		if (hud.targetMaster != targetMaster)
		{
			SetTargetMaster(hud.targetMaster);
		}
		if ((bool)currentNotification && (bool)notificationQueue)
		{
			currentNotification.SetNotificationT(notificationQueue.GetCurrentNotificationT());
		}
	}

	private void SetUpNotification(CharacterMasterNotificationQueue.NotificationInfo notificationInfo)
	{
		object previousData;
		if (notificationInfo.transformation != null)
		{
			GameObject lowerPricedChestsRegenTransformationNotificationPrefab = genericTransformationNotificationPrefab;
			switch (notificationInfo.transformation.transformationType)
			{
			case CharacterMasterNotificationQueue.TransformationType.CloverVoid:
				if ((bool)cloverVoidTransformationNotificationPrefab)
				{
					lowerPricedChestsRegenTransformationNotificationPrefab = cloverVoidTransformationNotificationPrefab;
				}
				break;
			case CharacterMasterNotificationQueue.TransformationType.ContagiousVoid:
				if ((bool)contagiousVoidTransformationNotificationPrefab)
				{
					lowerPricedChestsRegenTransformationNotificationPrefab = contagiousVoidTransformationNotificationPrefab;
				}
				break;
			case CharacterMasterNotificationQueue.TransformationType.Suppressed:
				if ((bool)suppressedTransformationNotificationPrefab)
				{
					lowerPricedChestsRegenTransformationNotificationPrefab = suppressedTransformationNotificationPrefab;
				}
				break;
			case CharacterMasterNotificationQueue.TransformationType.LunarSun:
				if ((bool)lunarSunTransformationNotificationPrefab)
				{
					lowerPricedChestsRegenTransformationNotificationPrefab = lunarSunTransformationNotificationPrefab;
				}
				break;
			case CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen:
				if ((bool)regeneratingScrapRegenTransformationNotificationPrefab)
				{
					lowerPricedChestsRegenTransformationNotificationPrefab = regeneratingScrapRegenTransformationNotificationPrefab;
				}
				break;
			case CharacterMasterNotificationQueue.TransformationType.SaleStarRegen:
				if ((bool)LowerPricedChestsRegenTransformationNotificationPrefab)
				{
					lowerPricedChestsRegenTransformationNotificationPrefab = LowerPricedChestsRegenTransformationNotificationPrefab;
				}
				break;
			case CharacterMasterNotificationQueue.TransformationType.TeleportOnLowHealthRegen:
				if ((bool)teleportOnLowHealthRegenTransformationNotificationPrefab)
				{
					lowerPricedChestsRegenTransformationNotificationPrefab = teleportOnLowHealthRegenTransformationNotificationPrefab;
				}
				break;
			}
			currentNotification = Object.Instantiate(lowerPricedChestsRegenTransformationNotificationPrefab).GetComponent<GenericNotification>();
			previousData = notificationInfo.transformation.previousData;
			if (!(previousData is ItemDef previousItem))
			{
				if (previousData is EquipmentDef previousEquipment)
				{
					currentNotification.SetPreviousEquipment(previousEquipment);
				}
			}
			else
			{
				currentNotification.SetPreviousItem(previousItem);
			}
		}
		else
		{
			currentNotification = Object.Instantiate(genericNotificationPrefab).GetComponent<GenericNotification>();
		}
		previousData = notificationInfo.data;
		if (!(previousData is ItemDef item))
		{
			if (!(previousData is EquipmentDef equipment))
			{
				if (previousData is ArtifactDef artifact)
				{
					currentNotification.SetArtifact(artifact);
				}
			}
			else
			{
				currentNotification.SetEquipment(equipment);
			}
		}
		else
		{
			currentNotification.SetItem(item);
		}
		currentNotification.GetComponent<RectTransform>().SetParent(GetComponent<RectTransform>(), worldPositionStays: false);
	}

	private void OnCurrentNotificationChanged(CharacterMasterNotificationQueue notificationQueue)
	{
		ShowCurrentNotification(notificationQueue);
	}

	private void ShowCurrentNotification(CharacterMasterNotificationQueue notificationQueue)
	{
		DestroyCurrentNotification();
		CharacterMasterNotificationQueue.NotificationInfo notificationInfo = notificationQueue.GetCurrentNotification();
		if (notificationInfo != null)
		{
			SetUpNotification(notificationInfo);
		}
	}

	private void DestroyCurrentNotification()
	{
		if ((bool)currentNotification)
		{
			Object.Destroy(currentNotification.gameObject);
			currentNotification = null;
		}
	}

	private void SetTargetMaster(CharacterMaster newMaster)
	{
		DestroyCurrentNotification();
		CleanUpCurrentMaster();
		targetMaster = newMaster;
		if ((bool)newMaster)
		{
			notificationQueue = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(newMaster);
			notificationQueue.onCurrentNotificationChanged += OnCurrentNotificationChanged;
			ShowCurrentNotification(notificationQueue);
		}
	}

	private void OnCharacterMasterLost(CharacterMaster master)
	{
		if ((object)master == targetMaster)
		{
			CleanUpCurrentMaster();
		}
	}

	private void CleanUpCurrentMaster()
	{
		if ((bool)notificationQueue)
		{
			notificationQueue.onCurrentNotificationChanged -= OnCurrentNotificationChanged;
		}
		notificationQueue = null;
		targetMaster = null;
	}
}
