using UnityEngine;

namespace RoR2;

public class SyncFlickerLightToAnimateShaderAlpha : MonoBehaviour
{
	[SerializeField]
	private AnimateShaderAlpha animateShaderAlpha;

	[SerializeField]
	private FlickerLight flickerLight;

	private float startingIntensity = 1f;

	private AnimationCurve acCurve;

	private void Start()
	{
		startingIntensity = animateShaderAlpha.alphaCurve.Evaluate(0f);
	}

	private void OnEnable()
	{
		flickerLight.UpdateLightIntensityMultiplier(startingIntensity);
		acCurve = animateShaderAlpha.alphaCurve;
	}

	private void Update()
	{
		if (animateShaderAlpha.enabled && flickerLight.enabled)
		{
			flickerLight.UpdateLightIntensityMultiplier(acCurve.Evaluate(animateShaderAlpha.time / animateShaderAlpha.timeMax));
		}
	}
}
