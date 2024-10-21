using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2.PostProcessing;

public class ScreenDamage : MonoBehaviour
{
	private CameraRigController cameraRigController;

	private PostProcessLayer postProcessLayer;

	private ScreenDamagePP postProcessSettings;

	[Header("Debug values")]
	public bool useDebugValues;

	public float debugTintValue;

	public float debugDesatValue;

	public float debugDistortionValue;

	[Header("Game-wide limits")]
	public float DistortionScale = 1f;

	public float DistortionPower = 1f;

	public float DesaturationScale = 1f;

	public float DesaturationPower = 1f;

	public float TintScale = 1f;

	public float TintPower = 1f;

	public float debugHealthPercentage;

	private float healthPercentage = 1f;

	private const float hitTintDecayTime = 0.6f;

	private const float hitTintScale = 1.6f;

	private const float deathWeight = 2f;

	private float oldDistortionStrength;

	private float oldDesaturationStrength;

	private float oldTintStrength;

	private ScreenDamageCalculatorDefault screenDamageCalculatorDefault = new ScreenDamageCalculatorDefault();

	private ScreenDamageCalculator workingScreenDamageCalculator;

	private void Awake()
	{
		InitPostProcessSettingsVars();
	}

	private void OnEnable()
	{
		InitPostProcessSettingsVars();
	}

	private void InitPostProcessSettingsVars()
	{
		if (!(postProcessSettings != null))
		{
			cameraRigController = GetComponentInParent<CameraRigController>();
			postProcessLayer = GetComponent<PostProcessLayer>();
			GetComponentInChildren<PostProcessVolume>().profile.TryGetSettings<ScreenDamagePP>(out postProcessSettings);
			if ((bool)postProcessSettings)
			{
				oldDistortionStrength = postProcessSettings.DistortionStrength.value;
				oldDesaturationStrength = postProcessSettings.DesaturationStrength.value;
				oldTintStrength = postProcessSettings.TintStrength.value;
			}
		}
	}

	public void Update()
	{
		InitPostProcessSettingsVars();
		if ((bool)cameraRigController)
		{
			if ((bool)cameraRigController.target)
			{
				HealthComponent component = cameraRigController.target.GetComponent<HealthComponent>();
				if (component != null && component.screenDamageCalculator != null)
				{
					workingScreenDamageCalculator = component.screenDamageCalculator;
				}
				else
				{
					workingScreenDamageCalculator = screenDamageCalculatorDefault;
				}
				workingScreenDamageCalculator.CalculateScreenDamage(this, component);
			}
			else
			{
				workingScreenDamageCalculator = screenDamageCalculatorDefault;
			}
			float num = (SettingsConVars.enableScreenDistortion.value ? DistortionScale : 0f);
			ScreenDamageData screenDamageData = workingScreenDamageCalculator.screenDamageData;
			postProcessSettings.DistortionStrength.Override(screenDamageData.distortionStrength * num);
			postProcessSettings.DesaturationStrength.Override(screenDamageData.desaturationStrength);
			postProcessSettings.TintStrength.Override(screenDamageData.tintStrength);
		}
		else
		{
			postProcessSettings.DistortionStrength.Override(0f);
			postProcessSettings.DesaturationStrength.Override(0f);
			postProcessSettings.TintStrength.Override(0f);
		}
		float value = postProcessSettings.DistortionStrength.value;
		float value2 = postProcessSettings.DesaturationStrength.value;
		float value3 = postProcessSettings.TintStrength.value;
		if (!Mathf.Approximately(oldDistortionStrength, value) || !Mathf.Approximately(oldDesaturationStrength, value2) || !Mathf.Approximately(oldTintStrength, value3))
		{
			postProcessLayer.OnVolumeSettingsChanged();
			oldDistortionStrength = postProcessSettings.DistortionStrength.value;
			oldDesaturationStrength = postProcessSettings.DesaturationStrength.value;
			oldTintStrength = postProcessSettings.TintStrength.value;
		}
	}
}
