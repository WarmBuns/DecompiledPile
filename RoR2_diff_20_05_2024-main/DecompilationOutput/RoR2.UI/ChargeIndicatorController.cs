using System.Text;
using HG;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class ChargeIndicatorController : MonoBehaviour
{
	[Header("Cached Values")]
	public SpriteRenderer[] iconSprites;

	public TextMeshPro chargingText;

	public TextMeshPro pingText;

	public HoldoutZoneController holdoutZoneController;

	public ObjectScaleCurve pingBounceAnimation;

	[Header("Color Values")]
	public Color spriteBaseColor;

	public Color spriteFlashColor;

	public Color spriteChargingColor;

	public Color spriteChargedColor;

	public Color textBaseColor;

	public Color textChargingColor;

	public Color playerPingColor;

	[Header("Options")]
	public bool disableTextWhenNotCharging;

	public bool disableTextWhenCharged;

	public bool flashWhenNotCharging;

	public float flashFrequency;

	[Header("Control Values - Don't assign via inspector!")]
	public bool isCharging;

	public bool isCharged;

	public bool isDiscovered;

	public bool isActivated;

	private float flashStopwatch;

	public bool isPinged;

	private UserProfile cachedUserProfile;

	private bool showIndicatorWhenDiscovered;

	public uint chargeValue { get; set; }

	private void Awake()
	{
		UserProfile userProfile = LocalUserManager.GetFirstLocalUser()?.userProfile;
		if (userProfile != null)
		{
			cachedUserProfile = userProfile;
		}
	}

	public void TriggerPing(string pingOwnerName, bool triggerAnimation = true)
	{
		if (!isActivated || isCharged)
		{
			pingText.SetText(pingOwnerName);
			isPinged = true;
			if (triggerAnimation)
			{
				TriggerPingAnimation();
			}
		}
	}

	public void TriggerPingAnimation()
	{
		if (cachedUserProfile != null)
		{
			showIndicatorWhenDiscovered = cachedUserProfile.useTeleporterDiscoveryIndicator;
		}
		if ((bool)pingBounceAnimation && (!isActivated || isCharged) && (showIndicatorWhenDiscovered || isPinged))
		{
			pingBounceAnimation.enabled = true;
			pingBounceAnimation.Reset();
		}
	}

	private void Update()
	{
		if ((bool)holdoutZoneController)
		{
			chargeValue = (uint)holdoutZoneController.displayChargePercent;
			isCharging = holdoutZoneController.isAnyoneCharging;
		}
		if (cachedUserProfile != null)
		{
			showIndicatorWhenDiscovered = cachedUserProfile.useTeleporterDiscoveryIndicator;
		}
		Color color = spriteBaseColor;
		Color color2 = textBaseColor;
		if (!isActivated && (isPinged || (isDiscovered && showIndicatorWhenDiscovered)))
		{
			color = (isPinged ? playerPingColor : spriteChargedColor);
			chargingText.enabled = false;
			pingText.enabled = isPinged;
		}
		else if (!isCharged)
		{
			pingText.enabled = false;
			if (flashWhenNotCharging)
			{
				flashStopwatch += Time.deltaTime;
				color = (((int)(flashStopwatch * flashFrequency) % 2 == 0) ? spriteFlashColor : spriteBaseColor);
			}
			if (isCharging)
			{
				color = spriteChargingColor;
				color2 = textChargingColor;
			}
			if (disableTextWhenNotCharging)
			{
				chargingText.enabled = isCharging;
			}
			else
			{
				chargingText.enabled = true;
			}
		}
		else
		{
			color = (isPinged ? playerPingColor : spriteChargedColor);
			chargingText.enabled = !disableTextWhenCharged && !isPinged;
			pingText.enabled = isPinged;
		}
		if (chargingText.enabled)
		{
			StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
			stringBuilder.AppendUint(chargeValue, 1u, 3u).Append("%");
			chargingText.SetText(stringBuilder);
			HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		}
		if (pingText.enabled)
		{
			pingText.color = playerPingColor;
		}
		SpriteRenderer[] array = iconSprites;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].color = color;
		}
		chargingText.color = color2;
	}
}
