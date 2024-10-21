using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2;

[RequireComponent(typeof(PostProcessVolume))]
public class HookLightingIntoPostProcessVolume : MonoBehaviour
{
	[Header("Required Values")]
	public PostProcessVolume volume;

	[ColorUsage(true, true)]
	public Color overrideAmbientColor;

	[Header("Optional Values")]
	public Light directionalLight;

	public Color overrideDirectionalColor;

	public ParticleSystem particleSystem;

	public float overrideParticleSystemMultiplier;

	private Collider[] volumeColliders;

	private Color defaultAmbientColor;

	private Color defaultDirectionalColor;

	private float defaultParticleSystemMultiplier;

	private bool hasCachedInitialValues;

	private void OnEnable()
	{
		volumeColliders = GetComponents<Collider>();
		if (!hasCachedInitialValues)
		{
			defaultAmbientColor = RenderSettings.ambientLight;
			if ((bool)directionalLight)
			{
				defaultDirectionalColor = directionalLight.color;
			}
			if ((bool)particleSystem)
			{
				defaultParticleSystemMultiplier = particleSystem.emission.rateOverTimeMultiplier;
			}
			hasCachedInitialValues = true;
		}
		SceneCamera.onSceneCameraPreRender += OnPreRenderSceneCam;
	}

	private void OnDisable()
	{
		SceneCamera.onSceneCameraPreRender -= OnPreRenderSceneCam;
	}

	private void OnPreRenderSceneCam(SceneCamera sceneCam)
	{
		float interpFactor = GetInterpFactor(sceneCam.camera.transform.position);
		RenderSettings.ambientLight = Color.Lerp(defaultAmbientColor, overrideAmbientColor, interpFactor);
		if ((bool)directionalLight)
		{
			directionalLight.color = Color.Lerp(defaultDirectionalColor, overrideDirectionalColor, interpFactor);
		}
		if ((bool)particleSystem)
		{
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.rateOverTimeMultiplier = Mathf.Lerp(defaultParticleSystemMultiplier, overrideParticleSystemMultiplier, interpFactor);
		}
	}

	private float GetInterpFactor(Vector3 triggerPos)
	{
		if (!volume.enabled || volume.weight <= 0f)
		{
			return 0f;
		}
		if (volume.isGlobal)
		{
			return 1f;
		}
		float num = 0f;
		Collider[] array = volumeColliders;
		foreach (Collider collider in array)
		{
			float num2 = float.PositiveInfinity;
			if (collider.enabled)
			{
				float sqrMagnitude = ((collider.ClosestPoint(triggerPos) - triggerPos) / 2f).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
				}
				float num3 = volume.blendDistance * volume.blendDistance;
				if (!(num2 > num3) && num3 > 0f)
				{
					num = Mathf.Max(num, 1f - num2 / num3);
				}
			}
		}
		return num;
	}
}
