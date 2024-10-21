using System;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class MoneyText : MonoBehaviour
{
	public TextMeshProUGUI targetText;

	public FlashPanel flashPanel;

	private int displayAmount;

	private float updateTimer;

	private float coinSoundCooldown;

	public int targetValue;

	public string sound = "Play_UI_coin";

	public void Update()
	{
		coinSoundCooldown -= Time.deltaTime;
		if (!targetText)
		{
			return;
		}
		if (updateTimer <= 0f)
		{
			int num = 0;
			if (displayAmount != targetValue)
			{
				num = Math.DivRem(targetValue - displayAmount, 3, out var result);
				if (result != 0)
				{
					num += Math.Sign(result);
				}
				if (num > 0)
				{
					if (coinSoundCooldown <= 0f)
					{
						coinSoundCooldown = 0.025f;
						Util.PlaySound(sound, RoR2Application.instance.gameObject);
					}
					if ((bool)flashPanel)
					{
						flashPanel.Flash();
					}
				}
				displayAmount += num;
			}
			float num2 = Mathf.Min(0.5f / (float)num, 0.25f);
			targetText.text = TextSerialization.ToStringNumeric(displayAmount);
			updateTimer = num2;
		}
		updateTimer -= Time.deltaTime;
	}
}
