using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class HUDBossHealthBarController : MonoBehaviour
{
	public HUD hud;

	public GameObject container;

	public Image fillRectImage;

	public Image delayRectImage;

	public SpriteAsNumberManager spriteAsNumberManager;

	public TextMeshProUGUI healthLabel;

	public TextMeshProUGUI bossNameLabel;

	public TextMeshProUGUI bossSubtitleLabel;

	private BossGroup currentBossGroup;

	private BossGroup oldBossGroup;

	private float delayedTotalHealthFraction;

	private float healthFractionVelocity;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	public Material healthMaterial;

	public Material delayMaterial;

	private HealthBar.BarInfo healthInfo;

	private HealthBar.BarInfo oldHealthInfo;

	private HealthBar.BarInfo delayInfo;

	private HealthBar.BarInfo oldDelayInfo;

	private int oldTotalHealth;

	private int oldTotalMaxHealth;

	private bool wasActive;

	private bool firstFrame;

	private string oldname = "";

	private string oldsub = "";

	private Run.TimeStamp nextAllowedSourceUpdateTime = Run.TimeStamp.negativeInfinity;

	private bool listeningForClientDamageNotified;

	private void Start()
	{
		if ((bool)healthLabel)
		{
			healthLabel.gameObject.SetActive(value: true);
		}
		if ((bool)spriteAsNumberManager)
		{
			spriteAsNumberManager.gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		List<BossGroup> instancesList = InstanceTracker.GetInstancesList<BossGroup>();
		int num = 0;
		for (int i = 0; i < instancesList.Count; i++)
		{
			if (instancesList[i].shouldDisplayHealthBarOnHud)
			{
				num++;
			}
		}
		SetListeningForClientDamageNotified(num > 1);
		if ((bool)currentBossGroup && !currentBossGroup.shouldDisplayHealthBarOnHud)
		{
			currentBossGroup = null;
		}
		if (num > 0)
		{
			if (num != 1 && (bool)currentBossGroup)
			{
				return;
			}
			for (int j = 0; j < instancesList.Count; j++)
			{
				if (instancesList[j].shouldDisplayHealthBarOnHud)
				{
					currentBossGroup = instancesList[j];
					break;
				}
			}
		}
		else
		{
			currentBossGroup = null;
		}
	}

	private void OnEnable()
	{
		healthInfo = default(HealthBar.BarInfo);
		oldHealthInfo = default(HealthBar.BarInfo);
		healthInfo.normalizedXMin = 0f;
		oldHealthInfo.normalizedXMin = -1f;
		healthInfo.enabled = true;
		healthInfo.sprite = (oldHealthInfo.sprite = fillRectImage.sprite);
		delayInfo = default(HealthBar.BarInfo);
		oldDelayInfo = default(HealthBar.BarInfo);
		delayInfo.normalizedXMin = 0f;
		oldDelayInfo.normalizedXMin = -1f;
		delayInfo.sprite = (oldDelayInfo.sprite = delayRectImage.sprite);
		delayInfo.enabled = true;
		oldBossGroup = null;
		oldname = "";
		oldsub = "";
	}

	private void OnDisable()
	{
		SetListeningForClientDamageNotified(newListeningForClientDamageNotified: false);
	}

	private void OnClientDamageNotified(DamageDealtMessage damageDealtMessage)
	{
		if (!nextAllowedSourceUpdateTime.hasPassed || !damageDealtMessage.victim)
		{
			return;
		}
		CharacterBody component = damageDealtMessage.victim.GetComponent<CharacterBody>();
		if ((bool)component && component.isBoss && (object)damageDealtMessage.attacker == hud.targetBodyObject)
		{
			BossGroup bossGroup = BossGroup.FindBossGroup(component);
			if ((bool)bossGroup && bossGroup.shouldDisplayHealthBarOnHud)
			{
				currentBossGroup = bossGroup;
				nextAllowedSourceUpdateTime = Run.TimeStamp.now + 1f;
			}
		}
	}

	private void SetListeningForClientDamageNotified(bool newListeningForClientDamageNotified)
	{
		if (newListeningForClientDamageNotified != listeningForClientDamageNotified)
		{
			listeningForClientDamageNotified = newListeningForClientDamageNotified;
			if (listeningForClientDamageNotified)
			{
				GlobalEventManager.onClientDamageNotified += OnClientDamageNotified;
			}
			else
			{
				GlobalEventManager.onClientDamageNotified -= OnClientDamageNotified;
			}
		}
	}

	private void SetRectPosition(RectTransform rectTransform, float xMin, float xMax, float sizeDelta)
	{
		rectTransform.anchorMin = new Vector2(xMin, 0f);
		rectTransform.anchorMax = new Vector2(xMax, 1f);
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.sizeDelta = new Vector2(sizeDelta * 0.5f + 1f, sizeDelta + 1f);
	}

	private void ApplyBars()
	{
		HandleBar(fillRectImage, ref healthInfo, ref oldHealthInfo);
		HandleBar(delayRectImage, ref delayInfo, ref oldDelayInfo);
		static void HandleBar(Image image, ref HealthBar.BarInfo barInfo, ref HealthBar.BarInfo oldInfo)
		{
			if (barInfo.enabled != image.gameObject.activeInHierarchy)
			{
				image.gameObject.SetActive(barInfo.enabled);
			}
			if (barInfo.enabled)
			{
				if (!oldInfo.DimsEqual(barInfo))
				{
					oldInfo.SetDims(barInfo);
					image.fillAmount = barInfo.normalizedXMax;
				}
				if (barInfo.normalizedXMin != oldInfo.normalizedXMin)
				{
					oldInfo.normalizedXMin = barInfo.normalizedXMin;
				}
				if (barInfo.normalizedXMax != oldInfo.normalizedXMax)
				{
					oldInfo.normalizedXMax = barInfo.normalizedXMax;
				}
			}
		}
	}

	private void LateUpdate()
	{
		bool flag = (bool)currentBossGroup && currentBossGroup.totalObservedHealth > 0f;
		if (wasActive != flag || !firstFrame)
		{
			container.SetActive(flag);
			wasActive = flag;
			oldname = "";
			oldsub = "";
			firstFrame = true;
		}
		if (flag)
		{
			float totalObservedHealth = currentBossGroup.totalObservedHealth;
			float totalMaxObservedMaxHealth = currentBossGroup.totalMaxObservedMaxHealth;
			float num = ((totalMaxObservedMaxHealth == 0f) ? 0f : Mathf.Clamp01(totalObservedHealth / totalMaxObservedMaxHealth));
			delayedTotalHealthFraction = Mathf.Clamp(Mathf.SmoothDamp(delayedTotalHealthFraction, num, ref healthFractionVelocity, 0.1f, float.PositiveInfinity, Time.deltaTime), num, 1f);
			healthInfo.normalizedXMax = num;
			delayInfo.normalizedXMax = delayedTotalHealthFraction;
			ApplyBars();
			int num2 = Mathf.FloorToInt(totalObservedHealth);
			int num3 = Mathf.FloorToInt(totalMaxObservedMaxHealth);
			if (num2 != oldTotalHealth || num3 != oldTotalMaxHealth)
			{
				oldTotalHealth = num2;
				oldTotalMaxHealth = num3;
				sharedStringBuilder.Clear().AppendInt(Mathf.FloorToInt(totalObservedHealth)).Append("/")
					.AppendInt(Mathf.FloorToInt(totalMaxObservedMaxHealth));
				healthLabel.SetText(sharedStringBuilder);
			}
			if (oldname.Length != currentBossGroup.bestObservedName.Length || oldsub.Length != currentBossGroup.bestObservedSubtitle.Length)
			{
				oldname = currentBossGroup.bestObservedName;
				oldsub = currentBossGroup.bestObservedSubtitle;
				oldBossGroup = currentBossGroup;
				bossNameLabel.SetText(currentBossGroup.bestObservedName);
				bossSubtitleLabel.SetText(currentBossGroup.bestObservedSubtitle);
			}
		}
	}
}
