using UnityEngine;

namespace RoR2.PostProcessing;

public class ScreenDamageCalculatorSojourn : ScreenDamageCalculator
{
	private float totalStrength;

	private float hitTint;

	private const float deathWeight = 0.5f;

	private const float hitTintDecayTime = 0.6f;

	private const float hitTintScale = 1.6f;

	private float healthPercentage;

	private ScreenDamageData _screenDamageData = new ScreenDamageData();

	public float minimumDamage = 0.2f;

	public float maximumDamage = 0.8f;

	public float duration = 4f;

	public bool _combineAdditively;

	private float timeStarted;

	private bool initialized;

	public override ScreenDamageData screenDamageData => _screenDamageData;

	public override void CalculateScreenDamage(ScreenDamage screenDamage, HealthComponent targetHealthComponent)
	{
		if (!initialized)
		{
			Initialize();
		}
		totalStrength = 0.5f;
		hitTint = 0f;
		float num = (Time.time - timeStarted) % duration / duration;
		if (num > 0.5f)
		{
			num = 0.5f - (num - 0.5f);
		}
		num /= 0.5f;
		healthPercentage = Mathf.Lerp(minimumDamage, maximumDamage, num);
		if (screenDamage.debugHealthPercentage != 0f)
		{
			healthPercentage = screenDamage.debugHealthPercentage;
		}
		_screenDamageData.distortionStrength = totalStrength * screenDamage.DistortionScale * Mathf.Pow(healthPercentage, screenDamage.DistortionPower);
		_screenDamageData.desaturationStrength = totalStrength * screenDamage.DesaturationScale * Mathf.Pow(healthPercentage, screenDamage.DesaturationPower);
		_screenDamageData.tintStrength = totalStrength * screenDamage.TintScale * (Mathf.Pow(healthPercentage, screenDamage.TintPower) + hitTint);
		ScreenDamagePPRenderer.ExtendTemporaryProps();
	}

	private void Initialize()
	{
		initialized = true;
		timeStarted = Time.time;
		if (_screenDamageData.tintColor == default(Color))
		{
			_screenDamageData.IntendedTintColor = Color.yellow;
		}
		ScreenDamagePPRenderer.SetTemporaryProps(this, _screenDamageData.colorParameter, null, _screenDamageData.combineAdditively);
	}

	public override void End()
	{
		if (initialized)
		{
			ScreenDamagePPRenderer.ClearTemporaryProps(this);
		}
	}
}
