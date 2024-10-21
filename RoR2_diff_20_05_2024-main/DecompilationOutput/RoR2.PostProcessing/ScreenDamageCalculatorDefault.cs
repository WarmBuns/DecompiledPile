using UnityEngine;

namespace RoR2.PostProcessing;

public class ScreenDamageCalculatorDefault : ScreenDamageCalculator
{
	private const float deathWeight = 0.5f;

	private const float hitTintDecayTime = 0.6f;

	private const float hitTintScale = 0.9f;

	private float healthPercentage;

	private float totalStrength;

	private float hitTint;

	private ScreenDamageData _screenDamageData = new ScreenDamageData(Color.red, 0f, 0f, 0f, _combineAdditively: false);

	public override ScreenDamageData screenDamageData => _screenDamageData;

	public bool combineAdditively => false;

	public override void CalculateScreenDamage(ScreenDamage screenDamage, HealthComponent targetHealthComponent)
	{
		totalStrength = 0.5f;
		hitTint = 0f;
		if ((bool)targetHealthComponent)
		{
			healthPercentage = Mathf.Clamp(targetHealthComponent.health / targetHealthComponent.fullHealth, 0f, 1f);
			hitTint = Mathf.Clamp01(1f - targetHealthComponent.timeSinceLastHit / 0.6f) * 0.9f;
			if (targetHealthComponent.health <= 0f)
			{
				totalStrength = 0f;
			}
		}
		if (screenDamage.debugHealthPercentage != 0f)
		{
			healthPercentage = screenDamage.debugHealthPercentage;
		}
		_screenDamageData.distortionStrength = totalStrength * screenDamage.DistortionScale * Mathf.Pow(1f - healthPercentage, screenDamage.DistortionPower);
		_screenDamageData.desaturationStrength = totalStrength * screenDamage.DesaturationScale * Mathf.Pow(1f - healthPercentage, screenDamage.DesaturationPower);
		_screenDamageData.tintStrength = totalStrength * screenDamage.TintScale * (Mathf.Pow(1f - healthPercentage, screenDamage.TintPower) + hitTint);
	}

	public override void End()
	{
	}
}
