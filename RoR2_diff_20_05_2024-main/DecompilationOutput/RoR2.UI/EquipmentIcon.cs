using System.Text;
using HG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class EquipmentIcon : MonoBehaviour
{
	private struct DisplayData
	{
		public EquipmentDef equipmentDef;

		public int cooldownValue;

		public int stock;

		public int maxStock;

		public bool hideEntireDisplay;

		public bool equipmentDisabled;

		public bool isReady => stock > 0;

		public bool hasEquipment => equipmentDef != null;

		public bool showCooldown
		{
			get
			{
				if (!isReady)
				{
					return hasEquipment;
				}
				return false;
			}
		}
	}

	public Inventory targetInventory;

	public EquipmentSlot targetEquipmentSlot;

	public GameObject displayRoot;

	public PlayerCharacterMasterController playerCharacterMasterController;

	public RawImage iconImage;

	public TextMeshProUGUI cooldownText;

	public TextMeshProUGUI stockText;

	public GameObject stockFlashPanelObject;

	public GameObject reminderFlashPanelObject;

	public GameObject isReadyPanelObject;

	public GameObject isAutoCastPanelObject;

	public TooltipProvider tooltipProvider;

	private HGButton NavButtonToEnable;

	public bool displayAlternateEquipment;

	private int previousStockCount;

	private float equipmentReminderTimer;

	public const string equipmentDisabledNameToken = "EQUIPMENT_DISABLED_NAME";

	public const string equipmentDisabledDescriptionToken = "EQUIPMENT_DISABLED_DESC";

	public static Texture equipmentDisabledSprite;

	private DisplayData currentDisplayData;

	private MPEventSystemLocator eventSystemLocator;

	private InspectPanelLocator inspectPanelLocator;

	private UserProfile userProfile;

	public bool hasEquipment => currentDisplayData.hasEquipment;

	public MPEventSystem eventSystem => eventSystemLocator.eventSystem;

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
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		NavButtonToEnable = GetComponent<HGButton>();
		inspectPanelLocator = GetComponent<InspectPanelLocator>();
		userProfile = GetComponentInParent<HUD>().localUserViewer.userProfile;
	}

	private void SetDisplayData(DisplayData newDisplayData)
	{
		if (!currentDisplayData.isReady && newDisplayData.isReady)
		{
			DoStockFlash();
		}
		if ((bool)displayRoot)
		{
			displayRoot.SetActive(!newDisplayData.hideEntireDisplay);
		}
		if (newDisplayData.stock > currentDisplayData.stock)
		{
			Util.PlaySound("Play_item_proc_equipMag", RoR2Application.instance.gameObject);
			DoStockFlash();
		}
		if ((bool)isReadyPanelObject)
		{
			isReadyPanelObject.SetActive(newDisplayData.isReady);
		}
		if ((bool)isAutoCastPanelObject)
		{
			if ((bool)targetInventory)
			{
				isAutoCastPanelObject.SetActive(targetInventory.GetItemCount(RoR2Content.Items.AutoCastEquipment) > 0);
			}
			else
			{
				isAutoCastPanelObject.SetActive(value: false);
			}
		}
		if ((bool)iconImage)
		{
			Texture texture = null;
			Color color = Color.clear;
			if ((object)newDisplayData.equipmentDef != null)
			{
				if (newDisplayData.equipmentDisabled)
				{
					color = Color.gray;
					texture = equipmentDisabledSprite;
				}
				else
				{
					color = ((newDisplayData.stock > 0) ? Color.white : Color.gray);
					texture = newDisplayData.equipmentDef.pickupIconTexture;
				}
			}
			iconImage.texture = texture;
			iconImage.color = color;
		}
		if ((bool)cooldownText)
		{
			if (newDisplayData.equipmentDisabled)
			{
				cooldownText.gameObject.SetActive(value: false);
			}
			else
			{
				cooldownText.gameObject.SetActive(newDisplayData.showCooldown);
				if (newDisplayData.cooldownValue != currentDisplayData.cooldownValue)
				{
					StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
					stringBuilder.AppendInt(newDisplayData.cooldownValue);
					cooldownText.SetText(stringBuilder);
					HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
				}
			}
		}
		if ((bool)stockText)
		{
			if (newDisplayData.hasEquipment && !newDisplayData.equipmentDisabled && (newDisplayData.maxStock > 1 || newDisplayData.stock > 1))
			{
				stockText.gameObject.SetActive(value: true);
				StringBuilder stringBuilder2 = HG.StringBuilderPool.RentStringBuilder();
				stringBuilder2.AppendInt(newDisplayData.stock);
				stockText.SetText(stringBuilder2);
				HG.StringBuilderPool.ReturnStringBuilder(stringBuilder2);
			}
			else
			{
				stockText.gameObject.SetActive(value: false);
			}
		}
		string titleToken = null;
		string bodyToken = null;
		Color titleColor = Color.white;
		Color gray = Color.gray;
		if (newDisplayData.equipmentDef != null)
		{
			if (newDisplayData.equipmentDisabled)
			{
				titleToken = "EQUIPMENT_DISABLED_NAME";
				bodyToken = "EQUIPMENT_DISABLED_DESC";
				titleColor = ColorCatalog.GetColor(newDisplayData.equipmentDef.colorIndex);
			}
			else
			{
				titleToken = newDisplayData.equipmentDef.nameToken;
				bodyToken = newDisplayData.equipmentDef.pickupToken;
				titleColor = ColorCatalog.GetColor(newDisplayData.equipmentDef.colorIndex);
			}
		}
		if ((bool)tooltipProvider)
		{
			tooltipProvider.titleToken = titleToken;
			tooltipProvider.titleColor = titleColor;
			tooltipProvider.bodyToken = bodyToken;
			tooltipProvider.bodyColor = gray;
		}
		if ((bool)NavButtonToEnable && newDisplayData.hasEquipment)
		{
			NavButtonToEnable.enabled = true;
		}
		currentDisplayData = newDisplayData;
	}

	private void DoReminderFlash()
	{
		if ((bool)reminderFlashPanelObject)
		{
			AnimateUIAlpha component = reminderFlashPanelObject.GetComponent<AnimateUIAlpha>();
			if ((bool)component)
			{
				component.time = 0f;
			}
			reminderFlashPanelObject.SetActive(value: true);
		}
		equipmentReminderTimer = 5f;
	}

	private void DoStockFlash()
	{
		DoReminderFlash();
		if ((bool)stockFlashPanelObject)
		{
			AnimateUIAlpha component = stockFlashPanelObject.GetComponent<AnimateUIAlpha>();
			if ((bool)component)
			{
				component.time = 0f;
			}
			stockFlashPanelObject.SetActive(value: true);
		}
	}

	private DisplayData GenerateDisplayData()
	{
		DisplayData result = default(DisplayData);
		EquipmentIndex equipmentIndex = EquipmentIndex.None;
		if ((bool)targetInventory)
		{
			EquipmentState equipmentState;
			if (displayAlternateEquipment)
			{
				equipmentState = targetInventory.alternateEquipmentState;
				result.hideEntireDisplay = targetInventory.GetEquipmentSlotCount() <= 1;
			}
			else
			{
				equipmentState = targetInventory.currentEquipmentState;
				result.hideEntireDisplay = false;
			}
			result.equipmentDisabled = targetInventory.GetEquipmentDisabled();
			_ = Run.FixedTimeStamp.now;
			Run.FixedTimeStamp chargeFinishTime = equipmentState.chargeFinishTime;
			equipmentIndex = equipmentState.equipmentIndex;
			result.cooldownValue = ((!chargeFinishTime.isInfinity) ? Mathf.CeilToInt(chargeFinishTime.timeUntilClamped) : 0);
			result.stock = equipmentState.charges;
			result.maxStock = ((!targetEquipmentSlot) ? 1 : targetEquipmentSlot.maxStock);
		}
		else if (displayAlternateEquipment)
		{
			result.hideEntireDisplay = true;
		}
		result.equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
		return result;
	}

	private void Start()
	{
		if (equipmentDisabledSprite == null)
		{
			HandleDisabledSpriteReference();
		}
	}

	private void HandleDisabledSpriteReference()
	{
		equipmentDisabledSprite = LegacyResourcesAPI.Load<Texture>("UI/texDisabledIcon");
	}

	private void Update()
	{
		SetDisplayData(GenerateDisplayData());
		equipmentReminderTimer -= Time.deltaTime;
		if (currentDisplayData.isReady && equipmentReminderTimer < 0f && currentDisplayData.equipmentDef != null)
		{
			DoReminderFlash();
		}
	}

	public bool TrySelect()
	{
		if (currentDisplayData.hasEquipment)
		{
			eventSystem.SetSelectedObject(base.gameObject);
			ItemClicked();
			return true;
		}
		return false;
	}

	public void ItemClicked()
	{
		if (userProfile.useInspectFeature && currentDisplayData.hasEquipment)
		{
			inspectPanel?.Show(currentDisplayData.equipmentDef);
		}
	}
}
