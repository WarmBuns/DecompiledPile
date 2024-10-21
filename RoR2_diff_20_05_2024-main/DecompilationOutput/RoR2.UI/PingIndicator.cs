using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class PingIndicator : MonoBehaviour
{
	public enum PingType
	{
		Default,
		Enemy,
		Interactable,
		Count
	}

	public PositionIndicator positionIndicator;

	public TextMeshPro pingText;

	public Highlight pingHighlight;

	public ObjectScaleCurve pingObjectScaleCurve;

	public GameObject positionIndicatorRoot;

	public Color textBaseColor;

	public GameObject[] defaultPingGameObjects;

	public Color defaultPingColor;

	public float defaultPingDuration;

	public GameObject[] enemyPingGameObjects;

	public Color enemyPingColor;

	public float enemyPingDuration;

	public GameObject[] interactablePingGameObjects;

	public Color interactablePingColor;

	public float interactablePingDuration;

	public static List<PingIndicator> instancesList = new List<PingIndicator>();

	private PingType pingType;

	private Color pingColor;

	private float pingDuration;

	private PurchaseInteraction pingTargetPurchaseInteraction;

	private HalcyoniteShrineInteractable halcyonShrine;

	private Transform targetTransformToFollow;

	private float fixedTimer;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	public Vector3 pingOrigin { get; set; }

	public Vector3 pingNormal { get; set; }

	public GameObject pingOwner { get; set; }

	public GameObject pingTarget { get; set; }

	public static Sprite GetInteractableIcon(GameObject gameObject)
	{
		PingInfoProvider component = gameObject.GetComponent<PingInfoProvider>();
		if ((bool)component && (bool)component.pingIconOverride)
		{
			return component.pingIconOverride;
		}
		string path = "Textures/MiscIcons/texMysteryIcon";
		if ((bool)gameObject.GetComponent<BarrelInteraction>())
		{
			path = "Textures/MiscIcons/texBarrelIcon";
		}
		else if (gameObject.name.Contains("Shrine"))
		{
			path = "Textures/MiscIcons/texShrineIconOutlined";
		}
		else if ((bool)gameObject.GetComponent<GenericPickupController>() || (bool)gameObject.GetComponent<PickupPickerController>())
		{
			path = "Textures/MiscIcons/texLootIconOutlined";
		}
		else if ((bool)gameObject.GetComponent<SummonMasterBehavior>())
		{
			path = "Textures/MiscIcons/texDroneIconOutlined";
		}
		else if ((bool)gameObject.GetComponent<TeleporterInteraction>())
		{
			path = "Textures/MiscIcons/texTeleporterIconOutlined";
		}
		else
		{
			PortalStatueBehavior component2 = gameObject.GetComponent<PortalStatueBehavior>();
			if ((bool)component2 && component2.portalType == PortalStatueBehavior.PortalType.Shop)
			{
				path = "Textures/MiscIcons/texMysteryIcon";
			}
			else
			{
				PurchaseInteraction component3 = gameObject.GetComponent<PurchaseInteraction>();
				if ((bool)component3 && component3.costType == CostTypeIndex.LunarCoin)
				{
					path = "Textures/MiscIcons/texMysteryIcon";
				}
				else if ((bool)component3 || (bool)gameObject.GetComponent<TimedChestController>())
				{
					path = "Textures/MiscIcons/texInventoryIconOutlined";
				}
			}
		}
		return LegacyResourcesAPI.Load<Sprite>(path);
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	[Conditional("ENABLE_PING_NAME_REPORTER")]
	private static void GenerateDebugMessageInChat(GameObject pingTarget)
	{
		if (pingTarget == null || !pingTarget)
		{
			return;
		}
		CharacterBody component = pingTarget.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			Chat.AddMessage("Debug: you pinged '" + component.gameObject.name.Replace("(Clone)", "") + "'");
			return;
		}
		if (pingTarget.TryGetComponent<IInspectable>(out var component2))
		{
			PickupDef pickupDef = PickupCatalog.GetPickupDef(component2.GetInspectInfoProvider().GetInfo().dynamicInspectPickupIndex);
			if (pickupDef != null)
			{
				ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
				if (pickupDef.displayPrefab != null)
				{
					Chat.AddMessage("Debug: you pinged '" + pickupDef.displayPrefab.gameObject.name + " ' (item name: " + ((itemDef != null) ? itemDef.name : "unknown") + "')");
					return;
				}
			}
		}
		Chat.AddMessage("Debug: you pinged '" + pingTarget.gameObject.name);
	}

	public void RebuildPing()
	{
		pingHighlight.enabled = false;
		base.transform.rotation = Util.QuaternionSafeLookRotation(pingNormal);
		base.transform.position = (pingTarget ? pingTarget.transform.position : pingOrigin);
		base.transform.localScale = Vector3.one;
		positionIndicator.targetTransform = (pingTarget ? pingTarget.transform : null);
		positionIndicator.defaultPosition = base.transform.position;
		IDisplayNameProvider displayNameProvider = (pingTarget ? pingTarget.GetComponentInParent<IDisplayNameProvider>() : null);
		CharacterBody characterBody = null;
		ModelLocator modelLocator = null;
		pingType = PingType.Default;
		pingObjectScaleCurve.enabled = false;
		pingObjectScaleCurve.enabled = true;
		GameObject[] array = defaultPingGameObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		array = enemyPingGameObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		array = interactablePingGameObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		if ((bool)pingTarget)
		{
			UnityEngine.Debug.LogFormat("Ping target {0}", pingTarget);
			modelLocator = pingTarget.GetComponent<ModelLocator>();
			if (displayNameProvider != null)
			{
				characterBody = pingTarget.GetComponent<CharacterBody>();
				if ((bool)characterBody)
				{
					pingType = PingType.Enemy;
					targetTransformToFollow = characterBody.coreTransform;
				}
				else
				{
					pingType = PingType.Interactable;
				}
			}
		}
		string ownerName = GetOwnerName();
		string text = (((MonoBehaviour)displayNameProvider) ? Util.GetBestBodyName(((MonoBehaviour)displayNameProvider).gameObject) : "");
		_ = pingTarget != null;
		pingText.enabled = true;
		pingText.text = ownerName;
		switch (pingType)
		{
		case PingType.Default:
		{
			pingColor = defaultPingColor;
			pingDuration = defaultPingDuration;
			pingHighlight.isOn = false;
			array = defaultPingGameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: true);
			}
			Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_DEFAULT"), ownerName));
			break;
		}
		case PingType.Enemy:
		{
			pingColor = enemyPingColor;
			pingDuration = enemyPingDuration;
			array = enemyPingGameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: true);
			}
			if (!modelLocator)
			{
				break;
			}
			Transform modelTransform = modelLocator.modelTransform;
			if ((bool)modelTransform)
			{
				CharacterModel component4 = modelTransform.GetComponent<CharacterModel>();
				if ((bool)component4)
				{
					bool flag = false;
					CharacterModel.RendererInfo[] baseRendererInfos = component4.baseRendererInfos;
					for (int i = 0; i < baseRendererInfos.Length; i++)
					{
						CharacterModel.RendererInfo rendererInfo = baseRendererInfos[i];
						if (!rendererInfo.ignoreOverlays && !flag)
						{
							pingHighlight.highlightColor = Highlight.HighlightColor.teleporter;
							pingHighlight.targetRenderer = rendererInfo.renderer;
							pingHighlight.strength = 1f;
							pingHighlight.isOn = true;
							pingHighlight.enabled = true;
							flag = true;
							break;
						}
					}
				}
			}
			Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_ENEMY"), ownerName, text));
			break;
		}
		case PingType.Interactable:
		{
			pingColor = interactablePingColor;
			pingDuration = interactablePingDuration;
			pingTargetPurchaseInteraction = pingTarget.GetComponent<PurchaseInteraction>();
			halcyonShrine = pingTarget.GetComponent<HalcyoniteShrineInteractable>();
			Sprite interactableIcon = GetInteractableIcon(pingTarget);
			SpriteRenderer component = interactablePingGameObjects[0].GetComponent<SpriteRenderer>();
			ShopTerminalBehavior component2 = pingTarget.GetComponent<ShopTerminalBehavior>();
			TeleporterInteraction component3 = pingTarget.GetComponent<TeleporterInteraction>();
			if ((bool)component2)
			{
				PickupIndex pickupIndex = component2.CurrentPickupIndex();
				text = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", text, (component2.pickupIndexIsHidden || !component2.pickupDisplay) ? "?" : Language.GetString(PickupCatalog.GetPickupDef(pickupIndex)?.nameToken ?? PickupCatalog.invalidPickupToken));
			}
			else if ((bool)component3)
			{
				pingDuration = 30f;
				pingText.enabled = false;
				component3.PingTeleporter(ownerName, this);
			}
			else if (!pingTarget.gameObject.name.Contains("Shrine") && ((bool)pingTarget.GetComponent<GenericPickupController>() || (bool)pingTarget.GetComponent<PickupPickerController>()))
			{
				pingDuration = 60f;
			}
			array = interactablePingGameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: true);
			}
			Renderer renderer = null;
			renderer = ((!modelLocator) ? pingTarget.GetComponentInChildren<Renderer>() : modelLocator.modelTransform.GetComponentInChildren<Renderer>());
			if ((bool)renderer)
			{
				pingHighlight.highlightColor = Highlight.HighlightColor.interactive;
				pingHighlight.targetRenderer = renderer;
				pingHighlight.strength = 1f;
				pingHighlight.isOn = true;
				pingHighlight.enabled = true;
			}
			component.sprite = interactableIcon;
			component.enabled = !component3;
			if ((bool)pingTargetPurchaseInteraction && pingTargetPurchaseInteraction.costType != 0)
			{
				sharedStringBuilder.Clear();
				CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(pingTargetPurchaseInteraction.costType);
				int num = pingTargetPurchaseInteraction.cost;
				if (pingTargetPurchaseInteraction.costType.Equals(CostTypeIndex.Money) && TeamManager.LongstandingSolitudesInParty() > 0)
				{
					num *= TeamManager.LongstandingSolitudesInParty() + 1;
				}
				costTypeDef.BuildCostStringStyled(num, sharedStringBuilder, forWorldDisplay: false);
				Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_INTERACTABLE_WITH_COST"), ownerName, text, sharedStringBuilder.ToString()));
			}
			else
			{
				Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_INTERACTABLE"), ownerName, text));
			}
			break;
		}
		}
		pingText.color = textBaseColor * pingColor;
		fixedTimer = pingDuration;
	}

	public string GetOwnerName()
	{
		if (!pingOwner)
		{
			return "";
		}
		return Util.GetBestMasterName(pingOwner.GetComponent<CharacterMaster>());
	}

	public void DestroyPing()
	{
		if ((bool)pingTarget)
		{
			pingTarget.GetComponent<TeleporterInteraction>()?.CancelTeleporterPing(this);
		}
		Object.Destroy(base.gameObject);
	}

	private void Update()
	{
		if (pingType == PingType.Interactable && (bool)pingTargetPurchaseInteraction && !pingTargetPurchaseInteraction.available && !halcyonShrine)
		{
			DestroyPing();
		}
		fixedTimer -= Time.deltaTime;
		if (fixedTimer <= 0f)
		{
			DestroyPing();
		}
	}

	private void LateUpdate()
	{
		if (!pingTarget)
		{
			if ((object)pingTarget != null)
			{
				DestroyPing();
			}
		}
		else if ((bool)targetTransformToFollow)
		{
			base.transform.SetPositionAndRotation(targetTransformToFollow.position, targetTransformToFollow.rotation);
		}
	}
}
