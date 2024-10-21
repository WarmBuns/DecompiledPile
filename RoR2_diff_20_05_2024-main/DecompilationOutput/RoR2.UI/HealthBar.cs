using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class HealthBar : MonoBehaviour
{
	public struct BarInfo
	{
		public bool enabled;

		public Color color;

		public Sprite sprite;

		public Image.Type imageType;

		public int index;

		public float normalizedXMin;

		public float normalizedXMax;

		public float sizeDelta;

		public bool DimsEqual(BarInfo rhs)
		{
			if (normalizedXMin == rhs.normalizedXMin && normalizedXMax == rhs.normalizedXMax)
			{
				return sizeDelta == rhs.sizeDelta;
			}
			return false;
		}

		public void SetDims(BarInfo rhs)
		{
			normalizedXMin = rhs.normalizedXMin;
			normalizedXMax = rhs.normalizedXMax;
			sizeDelta = rhs.sizeDelta;
		}
	}

	private struct BarInfoCollection
	{
		public BarInfo trailingUnderHealthbarInfo;

		public BarInfo instantHealthbarInfo;

		public BarInfo trailingOverHealthbarInfo;

		public BarInfo shieldBarInfo;

		public BarInfo curseBarInfo;

		public BarInfo barrierBarInfo;

		public BarInfo cullBarInfo;

		public BarInfo flashBarInfo;

		public BarInfo magneticBarInfo;

		public BarInfo ospBarInfo;

		public BarInfo lowHealthOverBarInfo;

		public BarInfo lowHealthUnderBarInfo;

		public void Init()
		{
			trailingUnderHealthbarInfo.index = -1;
			instantHealthbarInfo.index = -1;
			trailingOverHealthbarInfo.index = -1;
			shieldBarInfo.index = -1;
			curseBarInfo.index = -1;
			barrierBarInfo.index = -1;
			cullBarInfo.index = -1;
			flashBarInfo.index = -1;
			magneticBarInfo.index = -1;
			ospBarInfo.index = -1;
			lowHealthOverBarInfo.index = -1;
			lowHealthUnderBarInfo.index = -1;
		}

		public int GetActiveCount()
		{
			int count = 0;
			Check(ref lowHealthUnderBarInfo);
			Check(ref trailingUnderHealthbarInfo);
			Check(ref instantHealthbarInfo);
			Check(ref trailingOverHealthbarInfo);
			Check(ref shieldBarInfo);
			Check(ref curseBarInfo);
			Check(ref barrierBarInfo);
			Check(ref flashBarInfo);
			Check(ref cullBarInfo);
			Check(ref magneticBarInfo);
			Check(ref ospBarInfo);
			Check(ref lowHealthOverBarInfo);
			return count;
			void Check(ref BarInfo field)
			{
				count += (field.enabled ? 1 : 0);
			}
		}
	}

	private HealthComponent _source;

	private HealthComponent oldSource;

	public Material baseSubBarMat;

	public HealthBarStyle style;

	[Tooltip("The container rect for the actual bars.")]
	public RectTransform barContainer;

	public RectTransform eliteBackdropRectTransform;

	public Image criticallyHurtImage;

	public Image deadImage;

	public float maxLastHitTimer = 1f;

	public bool scaleHealthbarWidth;

	public float minHealthbarWidth;

	public float maxHealthbarWidth;

	public float minHealthbarHealth;

	public float maxHealthbarHealth;

	private float displayStringCurrentHealth;

	private float displayStringFullHealth;

	private RectTransform rectTransform;

	private float cachedFractionalValue = 1f;

	private float healthFractionVelocity;

	private bool healthCritical;

	private bool isInventoryCheckDirty = true;

	private bool hasLowHealthItem;

	private bool hasLowHealthBuff;

	public float updateDelay = 0.1f;

	private float updateTimer;

	[NonSerialized]
	public CharacterBody viewerBody;

	private static readonly Color infusionPanelColor = new Color32(231, 84, 58, byte.MaxValue);

	private static readonly Color voidPanelColor = new Color32(217, 123, byte.MaxValue, byte.MaxValue);

	private static readonly Color voidShieldsColor = new Color32(byte.MaxValue, 57, 199, byte.MaxValue);

	private float theta;

	private UIElementPairAllocator<Image, BarInfo> barAllocator;

	private BarInfoCollection barInfoCollection;

	private float oldHealth;

	private float oldFullHealth;

	private bool oldHealthCritical;

	private bool oldAlive = true;

	public TextMeshProUGUI currentHealthText;

	public TextMeshProUGUI fullHealthText;

	public SpriteAsNumberManager spriteAsNumberManager;

	public HealthComponent source
	{
		get
		{
			return _source;
		}
		set
		{
			if (_source != value)
			{
				RemoveInventoryChangedHandler();
				_source = value;
				healthFractionVelocity = 0f;
				cachedFractionalValue = (_source ? (_source.health / _source.fullCombinedHealth) : 0f);
				AddInventoryChangedHandler();
				isInventoryCheckDirty = true;
			}
		}
	}

	private void Awake()
	{
		updateDelay = 0f;
		rectTransform = (RectTransform)base.transform;
		barAllocator = new UIElementPairAllocator<Image, BarInfo>(barContainer, style.barPrefab);
	}

	private void Start()
	{
		barInfoCollection.Init();
		UpdateHealthbar(0f);
	}

	public void Update()
	{
		updateTimer -= Time.deltaTime;
		if (_source != oldSource)
		{
			updateTimer = 0f;
			oldSource = _source;
		}
		if (updateTimer <= 0f)
		{
			UpdateHealthbar(updateDelay - updateTimer);
			updateTimer = updateDelay;
		}
	}

	private void OnEnable()
	{
		AddInventoryChangedHandler();
		isInventoryCheckDirty = true;
	}

	private void OnDisable()
	{
		RemoveInventoryChangedHandler();
	}

	private void SetRectPosition(RectTransform rectTransform, float xMin, float xMax, float sizeDelta)
	{
		rectTransform.anchorMin = new Vector2(xMin, 0f);
		rectTransform.anchorMax = new Vector2(xMax, 1f);
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.sizeDelta = new Vector2(sizeDelta * 0.5f + 1f, sizeDelta + 1f);
	}

	public void UpdateBarRects()
	{
		int i = 0;
		barAllocator.AllocateElements(barInfoCollection.GetActiveCount());
		HandleBar(ref barInfoCollection.lowHealthUnderBarInfo);
		HandleBar(ref barInfoCollection.trailingUnderHealthbarInfo);
		HandleBar(ref barInfoCollection.instantHealthbarInfo);
		HandleBar(ref barInfoCollection.trailingOverHealthbarInfo);
		HandleBar(ref barInfoCollection.shieldBarInfo);
		HandleBar(ref barInfoCollection.curseBarInfo);
		HandleBar(ref barInfoCollection.barrierBarInfo);
		HandleBar(ref barInfoCollection.flashBarInfo);
		HandleBar(ref barInfoCollection.cullBarInfo);
		HandleBar(ref barInfoCollection.magneticBarInfo);
		HandleBar(ref barInfoCollection.ospBarInfo);
		HandleBar(ref barInfoCollection.lowHealthOverBarInfo);
		void HandleBar(ref BarInfo barInfo)
		{
			if (barInfo.enabled)
			{
				barInfo.index = i;
				Image image = barAllocator.elements[i];
				BarInfo barInfo2 = barAllocator.elementsData[i];
				if (!barInfo2.DimsEqual(barInfo))
				{
					barInfo2.SetDims(barInfo);
					SetRectPosition((RectTransform)image.transform, barInfo.normalizedXMin, barInfo.normalizedXMax, barInfo.sizeDelta);
				}
				int num = i + 1;
				i = num;
			}
		}
	}

	private void ApplyBars()
	{
		int i = 0;
		barAllocator.AllocateElements(barInfoCollection.GetActiveCount());
		HandleBar(ref barInfoCollection.lowHealthUnderBarInfo);
		HandleBar(ref barInfoCollection.trailingUnderHealthbarInfo);
		HandleBar(ref barInfoCollection.instantHealthbarInfo);
		HandleBar(ref barInfoCollection.trailingOverHealthbarInfo);
		HandleBar(ref barInfoCollection.shieldBarInfo);
		HandleBar(ref barInfoCollection.curseBarInfo);
		HandleBar(ref barInfoCollection.barrierBarInfo);
		HandleBar(ref barInfoCollection.flashBarInfo);
		HandleBar(ref barInfoCollection.cullBarInfo);
		HandleBar(ref barInfoCollection.magneticBarInfo);
		HandleBar(ref barInfoCollection.ospBarInfo);
		HandleBar(ref barInfoCollection.lowHealthOverBarInfo);
		void HandleBar(ref BarInfo barInfo)
		{
			if (barInfo.enabled)
			{
				Image image = barAllocator.elements[i];
				BarInfo val = barAllocator.elementsData[i];
				if (!val.DimsEqual(barInfo))
				{
					val.SetDims(barInfo);
					SetRectPosition((RectTransform)image.transform, barInfo.normalizedXMin, barInfo.normalizedXMax, barInfo.sizeDelta);
				}
				if (barInfo.imageType != val.imageType)
				{
					val.imageType = barInfo.imageType;
					image.type = barInfo.imageType;
				}
				if (barInfo.sprite != val.sprite)
				{
					val.sprite = barInfo.sprite;
					image.sprite = barInfo.sprite;
				}
				if (barInfo.color != image.color)
				{
					val.color = barInfo.color;
					image.color = barInfo.color;
				}
				if (barInfo.normalizedXMin != val.normalizedXMin)
				{
					val.normalizedXMin = barInfo.normalizedXMin;
				}
				if (barInfo.normalizedXMax != val.normalizedXMax)
				{
					val.normalizedXMax = barInfo.normalizedXMax;
				}
				barAllocator.SetData(val, i);
				int num = i + 1;
				i = num;
			}
		}
	}

	private void UpdateBarInfos()
	{
		float currentBarEnd;
		if ((bool)source)
		{
			if (isInventoryCheckDirty)
			{
				CheckInventory();
			}
			if ((bool)source && (bool)source.body)
			{
				hasLowHealthBuff = source.body.HasBuff(DLC2Content.Buffs.RevitalizeBuff);
			}
			HealthComponent.HealthBarValues healthBarValues = source.GetHealthBarValues();
			currentBarEnd = 0f;
			_ = source.fullCombinedHealth;
			ref BarInfo lowHealthUnderBarInfo = ref barInfoCollection.lowHealthUnderBarInfo;
			lowHealthUnderBarInfo.enabled = (hasLowHealthItem || hasLowHealthBuff) && source.isHealthLow;
			lowHealthUnderBarInfo.normalizedXMin = 0f;
			lowHealthUnderBarInfo.normalizedXMax = HealthComponent.lowHealthFraction * (1f - healthBarValues.curseFraction);
			ApplyStyle(ref lowHealthUnderBarInfo, ref style.lowHealthUnderStyle);
			cachedFractionalValue = Mathf.SmoothDamp(cachedFractionalValue, healthBarValues.healthFraction, ref healthFractionVelocity, 0.2f, float.PositiveInfinity, Time.deltaTime);
			ref BarInfo trailingUnderHealthbarInfo = ref barInfoCollection.trailingUnderHealthbarInfo;
			trailingUnderHealthbarInfo.normalizedXMin = 0f;
			trailingUnderHealthbarInfo.normalizedXMax = Mathf.Max(cachedFractionalValue, healthBarValues.healthFraction);
			trailingUnderHealthbarInfo.enabled = !trailingUnderHealthbarInfo.normalizedXMax.Equals(trailingUnderHealthbarInfo.normalizedXMin);
			ApplyStyle(ref trailingUnderHealthbarInfo, ref style.trailingUnderHealthBarStyle);
			ref BarInfo instantHealthbarInfo = ref barInfoCollection.instantHealthbarInfo;
			instantHealthbarInfo.enabled = healthBarValues.healthFraction > 0f;
			ApplyStyle(ref instantHealthbarInfo, ref style.instantHealthBarStyle);
			AddBar(ref instantHealthbarInfo, healthBarValues.healthFraction);
			ref BarInfo trailingOverHealthbarInfo = ref barInfoCollection.trailingOverHealthbarInfo;
			trailingOverHealthbarInfo.normalizedXMin = 0f;
			trailingOverHealthbarInfo.normalizedXMax = Mathf.Min(cachedFractionalValue + 0.01f, healthBarValues.healthFraction);
			trailingOverHealthbarInfo.enabled = !trailingOverHealthbarInfo.normalizedXMax.Equals(trailingOverHealthbarInfo.normalizedXMin);
			ApplyStyle(ref trailingOverHealthbarInfo, ref style.trailingOverHealthBarStyle);
			if (healthBarValues.isVoid)
			{
				trailingOverHealthbarInfo.color = voidPanelColor;
			}
			if (healthBarValues.isBoss || healthBarValues.hasInfusion)
			{
				trailingOverHealthbarInfo.color = infusionPanelColor;
			}
			if (healthCritical && style.flashOnHealthCritical)
			{
				trailingOverHealthbarInfo.color = GetCriticallyHurtColor();
			}
			ref BarInfo shieldBarInfo = ref barInfoCollection.shieldBarInfo;
			shieldBarInfo.enabled = healthBarValues.shieldFraction > 0f;
			ApplyStyle(ref shieldBarInfo, ref style.shieldBarStyle);
			if (healthBarValues.hasVoidShields)
			{
				shieldBarInfo.color = voidShieldsColor;
			}
			AddBar(ref shieldBarInfo, healthBarValues.shieldFraction);
			ref BarInfo curseBarInfo = ref barInfoCollection.curseBarInfo;
			curseBarInfo.enabled = healthBarValues.curseFraction > 0f;
			ApplyStyle(ref curseBarInfo, ref style.curseBarStyle);
			curseBarInfo.normalizedXMin = 1f - healthBarValues.curseFraction;
			curseBarInfo.normalizedXMax = 1f;
			ref BarInfo barrierBarInfo = ref barInfoCollection.barrierBarInfo;
			barrierBarInfo.enabled = source.barrier > 0f;
			ApplyStyle(ref barrierBarInfo, ref style.barrierBarStyle);
			barrierBarInfo.normalizedXMin = 0f;
			barrierBarInfo.normalizedXMax = healthBarValues.barrierFraction;
			ref BarInfo magneticBarInfo = ref barInfoCollection.magneticBarInfo;
			magneticBarInfo.enabled = source.magnetiCharge > 0f;
			magneticBarInfo.normalizedXMin = 0f;
			magneticBarInfo.normalizedXMax = healthBarValues.magneticFraction;
			magneticBarInfo.color = new Color(75f, 0f, 130f);
			ApplyStyle(ref magneticBarInfo, ref style.magneticStyle);
			float num = healthBarValues.cullFraction;
			if (healthBarValues.isElite && (bool)viewerBody)
			{
				num = Mathf.Max(num, viewerBody.executeEliteHealthFraction);
			}
			ref BarInfo cullBarInfo = ref barInfoCollection.cullBarInfo;
			cullBarInfo.enabled = num > 0f;
			cullBarInfo.normalizedXMin = 0f;
			cullBarInfo.normalizedXMax = num;
			ApplyStyle(ref cullBarInfo, ref style.cullBarStyle);
			float ospFraction = healthBarValues.ospFraction;
			ref BarInfo ospBarInfo = ref barInfoCollection.ospBarInfo;
			ospBarInfo.enabled = ospFraction > 0f;
			ospBarInfo.normalizedXMin = 0f;
			ospBarInfo.normalizedXMax = ospFraction;
			ApplyStyle(ref ospBarInfo, ref style.ospStyle);
			ref BarInfo lowHealthOverBarInfo = ref barInfoCollection.lowHealthOverBarInfo;
			lowHealthOverBarInfo.enabled = (hasLowHealthItem || hasLowHealthBuff) && !source.isHealthLow;
			lowHealthOverBarInfo.normalizedXMin = HealthComponent.lowHealthFraction * (1f - healthBarValues.curseFraction);
			lowHealthOverBarInfo.normalizedXMax = HealthComponent.lowHealthFraction * (1f - healthBarValues.curseFraction) + 0.005f;
			ApplyStyle(ref lowHealthOverBarInfo, ref style.lowHealthOverStyle);
		}
		void AddBar(ref BarInfo barInfo, float fraction)
		{
			if (barInfo.enabled)
			{
				barInfo.normalizedXMin = currentBarEnd;
				currentBarEnd = (barInfo.normalizedXMax = barInfo.normalizedXMin + fraction);
			}
		}
		static void ApplyStyle(ref BarInfo barInfo, ref HealthBarStyle.BarStyle barStyle)
		{
			barInfo.enabled &= barStyle.enabled;
			barInfo.color = barStyle.baseColor;
			barInfo.sprite = barStyle.sprite;
			barInfo.imageType = barStyle.imageType;
			barInfo.sizeDelta = barStyle.sizeDelta;
		}
	}

	public void InitializeHealthBar()
	{
		float num = 1f;
		if (!source)
		{
			return;
		}
		CharacterBody body = source.body;
		bool isElite = body.isElite;
		float fullHealth = source.fullHealth;
		if ((bool)eliteBackdropRectTransform)
		{
			if (isElite)
			{
				num += 1f;
			}
			eliteBackdropRectTransform.gameObject.SetActive(isElite);
		}
		if (scaleHealthbarWidth && (bool)body)
		{
			float x = Util.Remap(Mathf.Clamp((body.baseMaxHealth + body.baseMaxShield) * num, 0f, maxHealthbarHealth), minHealthbarHealth, maxHealthbarHealth, minHealthbarWidth, maxHealthbarWidth);
			rectTransform.sizeDelta = new Vector2(x, rectTransform.sizeDelta.y);
		}
		if ((bool)spriteAsNumberManager)
		{
			float num2 = Mathf.Ceil(source.combinedHealth);
			float num3 = Mathf.Ceil(fullHealth);
			if (num2 != displayStringCurrentHealth || num3 != displayStringFullHealth)
			{
				displayStringFullHealth = num3;
				displayStringCurrentHealth = num2;
				spriteAsNumberManager.SetHitPointValues((int)displayStringCurrentHealth, (int)displayStringFullHealth);
			}
		}
		else
		{
			if ((bool)currentHealthText)
			{
				float num4 = Mathf.Ceil(source.combinedHealth);
				if (num4 != displayStringCurrentHealth)
				{
					displayStringCurrentHealth = num4;
					currentHealthText.text = num4.ToString();
				}
			}
			if ((bool)fullHealthText)
			{
				float num5 = Mathf.Ceil(fullHealth);
				if (num5 != displayStringFullHealth)
				{
					displayStringFullHealth = num5;
					fullHealthText.text = num5.ToString();
				}
			}
		}
		healthCritical = source.isHealthLow && source.alive;
		if ((bool)criticallyHurtImage)
		{
			if (healthCritical)
			{
				criticallyHurtImage.enabled = true;
				criticallyHurtImage.color = GetCriticallyHurtColor();
			}
			else
			{
				criticallyHurtImage.enabled = false;
			}
		}
		if ((bool)deadImage)
		{
			deadImage.enabled = !source.alive;
		}
	}

	public void UpdateHealthbar(float deltaTime)
	{
		float num = 1f;
		if ((bool)source)
		{
			_ = source.body.isElite;
			float fullHealth = source.fullHealth;
			float num2 = source.fullHealth + source.fullShield;
			Mathf.Clamp01(source.health / num2 * num);
			Mathf.Clamp01(source.shield / num2 * num);
			if ((bool)spriteAsNumberManager)
			{
				float num3 = Mathf.Ceil(source.combinedHealth);
				float num4 = Mathf.Ceil(fullHealth);
				if (num3 != displayStringCurrentHealth || num4 != displayStringFullHealth)
				{
					displayStringFullHealth = num4;
					displayStringCurrentHealth = num3;
					spriteAsNumberManager.SetHitPointValues((int)displayStringCurrentHealth, (int)displayStringFullHealth);
				}
			}
			else
			{
				if ((bool)currentHealthText && source.combinedHealth != oldHealth)
				{
					oldHealth = source.combinedHealth;
					float num5 = Mathf.Ceil(source.combinedHealth);
					if (num5 != displayStringCurrentHealth)
					{
						displayStringCurrentHealth = num5;
						currentHealthText.text = num5.ToString();
					}
				}
				if ((bool)fullHealthText && fullHealth != oldFullHealth)
				{
					oldFullHealth = fullHealth;
					float num6 = Mathf.Ceil(fullHealth);
					if (num6 != displayStringFullHealth)
					{
						displayStringFullHealth = num6;
						fullHealthText.text = num6.ToString();
					}
				}
			}
			healthCritical = source.isHealthLow && source.alive;
			if ((bool)criticallyHurtImage && healthCritical != oldHealthCritical)
			{
				oldHealthCritical = healthCritical;
				if (healthCritical)
				{
					criticallyHurtImage.enabled = true;
					criticallyHurtImage.color = GetCriticallyHurtColor();
				}
				else
				{
					criticallyHurtImage.enabled = false;
				}
			}
			if ((bool)deadImage && source.alive != oldAlive)
			{
				oldAlive = source.alive;
				deadImage.enabled = !source.alive;
			}
		}
		else if ((bool)deadImage && oldAlive)
		{
			oldAlive = false;
			deadImage.enabled = true;
		}
		UpdateBarInfos();
		ApplyBars();
	}

	private void AddInventoryChangedHandler()
	{
		if ((bool)source?.body?.inventory)
		{
			source.body.inventory.onInventoryChanged += OnInventoryChanged;
		}
	}

	private void RemoveInventoryChangedHandler()
	{
		if ((bool)source?.body?.inventory)
		{
			source.body.inventory.onInventoryChanged -= OnInventoryChanged;
		}
	}

	private void OnInventoryChanged()
	{
		isInventoryCheckDirty = true;
	}

	private void CheckInventory()
	{
		isInventoryCheckDirty = false;
		Inventory inventory = source?.body?.inventory;
		if (!inventory)
		{
			return;
		}
		hasLowHealthItem = false;
		foreach (ItemIndex item in ItemCatalog.GetItemsWithTag(ItemTag.LowHealth))
		{
			if (inventory.GetItemCount(item) > 0)
			{
				hasLowHealthItem = true;
				break;
			}
		}
	}

	public static Color GetCriticallyHurtColor()
	{
		if (Mathf.FloorToInt(Time.fixedTime * 10f) % 2 != 0)
		{
			return ColorCatalog.GetColor(ColorCatalog.ColorIndex.Teleporter);
		}
		return Color.white;
	}
}
