using UnityEngine;

namespace RoR2;

public class SoulSpiralOrbiter
{
	private float degreesRotated;

	public float easeInDuration;

	public float boostEaseDuration;

	public float boostDuration;

	public float speedBoostCoefficient;

	public float damageMultiplier;

	private int boostCount;

	private bool doBoost;

	private bool endBoost;

	private float baseDegreesPerSecond;

	private float targetAdditionalDegreesPerSecond;

	private float currentAdditionalDegreesPerSecond;

	private float baseProjectileDamage;

	private float currentDamageMultiplier;

	private float prevTimeStamp;

	private float deltaTime;

	private float boostChangedTimeStamp;

	private int length;

	private SeekerController seekerController;

	private SeekerSoulSpiralManager soulSpiralManager;

	public SoulSpiralOrbiter(SeekerController seekerController, SeekerSoulSpiralManager soulSpiralManager)
	{
		this.seekerController = seekerController;
		this.soulSpiralManager = soulSpiralManager;
		prevTimeStamp = Time.fixedTime;
		degreesRotated = 0f;
		baseProjectileDamage = 1f;
	}

	public void ResetValues(SoulSpiralProjectile soulSpiralInstance)
	{
		baseDegreesPerSecond = soulSpiralInstance.degreesPerSecond;
		boostEaseDuration = soulSpiralInstance.boostEaseDuration;
		boostDuration = soulSpiralInstance.boostDuration;
		damageMultiplier = soulSpiralInstance.damageMultiplier;
		easeInDuration = soulSpiralInstance.easeInDuration;
		speedBoostCoefficient = soulSpiralInstance.speedBoostCoefficient;
	}

	public void UpdateSpirals(ref SoulSpiralProjectile[] array)
	{
		length = array.Length;
		UpdateCurrentSpeedAndDamage(Time.fixedTime);
		for (int i = 0; i < length; i++)
		{
			if (!(array[i] == null))
			{
				array[i].UpdateProjectile(prevTimeStamp, degreesRotated, currentDamageMultiplier, doBoost, endBoost);
			}
		}
		doBoost = false;
		endBoost = false;
	}

	public void UpdateCurrentSpeedAndDamage(float _currentTime)
	{
		deltaTime = _currentTime - prevTimeStamp;
		float boostTime = _currentTime - boostChangedTimeStamp;
		HandleBoostExpiring(_currentTime, ref boostTime);
		float num = 1f;
		if (boostCount > 0)
		{
			targetAdditionalDegreesPerSecond = speedBoostCoefficient * (float)boostCount / (speedBoostCoefficient * (float)boostCount + 1f);
			targetAdditionalDegreesPerSecond *= baseDegreesPerSecond;
			num = (targetAdditionalDegreesPerSecond + baseDegreesPerSecond) / baseDegreesPerSecond;
		}
		else
		{
			targetAdditionalDegreesPerSecond = 0f;
		}
		currentAdditionalDegreesPerSecond = Mathf.Lerp(currentAdditionalDegreesPerSecond, targetAdditionalDegreesPerSecond, boostTime / boostEaseDuration);
		degreesRotated += deltaTime * (baseDegreesPerSecond + currentAdditionalDegreesPerSecond);
		currentDamageMultiplier = baseProjectileDamage + (num - 1f) * damageMultiplier * baseProjectileDamage;
		prevTimeStamp = _currentTime;
	}

	private void HandleBoostExpiring(float stopWatchTime, ref float boostTime)
	{
		if (boostCount != 0 && boostTime > boostDuration)
		{
			boostCount = 0;
			boostChangedTimeStamp = stopWatchTime;
			boostTime = 0f;
			endBoost = true;
			soulSpiralManager.EndBoostFX();
			seekerController.SpiralSendCommand(SeekerSoulSpiralManager.SoulSpiralCommandType.EndBoost);
		}
	}

	public void BoostAuthority()
	{
		boostCount++;
		boostChangedTimeStamp = Time.fixedTime;
		doBoost = true;
		seekerController.SpiralSendCommand(SeekerSoulSpiralManager.SoulSpiralCommandType.Boost);
	}
}
