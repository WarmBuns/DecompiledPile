using UnityEngine;

namespace RoR2.PostProcessing;

public class ScreenDamageCalculatorDisabledSkills : ScreenDamageCalculator
{
	private const float deathWeight = 0.5f;

	private const float hitTintDecayTime = 0.6f;

	private const float hitTintScale = 0.9f;

	private const float deSaturationBase = 0.8f;

	private const float tintBase = 0.03f;

	private const float tintMaximum = 0.17f;

	private float healthPercentage;

	private float totalStrength;

	private float hitTint;

	private ScreenDamageData _screenDamageData = new ScreenDamageData(Color.grey, 0f, 0.7f, 0.3f, _combineAdditively: false);

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
		float distortionStrength = totalStrength * screenDamage.DistortionScale * Mathf.Pow(1f - healthPercentage, screenDamage.DistortionPower);
		if (screenDamage.useDebugValues)
		{
			distortionStrength = screenDamage.debugDistortionValue;
		}
		_screenDamageData.distortionStrength = distortionStrength;
		distortionStrength = 0.8f;
		if (screenDamage.useDebugValues)
		{
			distortionStrength = screenDamage.debugDesatValue;
		}
		_screenDamageData.desaturationStrength = distortionStrength;
		distortionStrength = 0.03f;
		distortionStrength += (totalStrength * screenDamage.TintScale + 0.2f) * (Mathf.Pow(1f - healthPercentage, screenDamage.TintPower) + hitTint);
		distortionStrength = Mathf.Min(distortionStrength, 0.17f);
		if (screenDamage.useDebugValues)
		{
			distortionStrength = screenDamage.debugTintValue;
		}
		_screenDamageData.tintStrength = distortionStrength;
		ScreenDamagePPRenderer.ExtendTemporaryProps();
	}

	public override void End()
	{
		ScreenDamagePPRenderer.ClearTemporaryProps(this);
	}
}
