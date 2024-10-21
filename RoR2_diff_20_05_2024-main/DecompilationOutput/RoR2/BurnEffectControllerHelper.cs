using UnityEngine;

namespace RoR2;

public class BurnEffectControllerHelper : MonoBehaviour
{
	public ParticleSystem burnParticleSystem;

	public DestroyOnTimer destroyOnTimer;

	public LightIntensityCurve lightIntensityCurve;

	public NormalizeParticleScale normalizeParticleScale;

	public BoneParticleController boneParticleController;

	private void Awake()
	{
		if (!burnParticleSystem)
		{
			burnParticleSystem = GetComponent<ParticleSystem>();
		}
		if (!destroyOnTimer)
		{
			destroyOnTimer = GetComponent<DestroyOnTimer>();
		}
		if (!lightIntensityCurve)
		{
			lightIntensityCurve = GetComponentInChildren<LightIntensityCurve>();
		}
		if (!normalizeParticleScale)
		{
			normalizeParticleScale = GetComponentInChildren<NormalizeParticleScale>();
		}
		if (!boneParticleController)
		{
			boneParticleController = GetComponentInChildren<BoneParticleController>();
		}
	}

	private void OnEnable()
	{
		if ((bool)lightIntensityCurve)
		{
			lightIntensityCurve.enabled = false;
		}
	}

	public void InitializeBurnEffect(Renderer modelRenderer)
	{
		if (!burnParticleSystem || !modelRenderer)
		{
			return;
		}
		ParticleSystem.ShapeModule shape = burnParticleSystem.shape;
		if (modelRenderer is MeshRenderer meshRenderer)
		{
			shape.shapeType = ParticleSystemShapeType.MeshRenderer;
			shape.meshRenderer = meshRenderer;
		}
		else if (modelRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
		{
			shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
			shape.skinnedMeshRenderer = skinnedMeshRenderer;
			if ((bool)boneParticleController)
			{
				boneParticleController.skinnedMeshRenderer = skinnedMeshRenderer;
			}
		}
		if ((bool)normalizeParticleScale)
		{
			normalizeParticleScale.UpdateParticleSystem();
		}
		burnParticleSystem.gameObject.SetActive(value: true);
	}

	public void EndEffect()
	{
		if ((bool)burnParticleSystem)
		{
			ParticleSystem.EmissionModule emission = burnParticleSystem.emission;
			emission.enabled = false;
		}
		if ((bool)destroyOnTimer)
		{
			destroyOnTimer.enabled = true;
		}
		if ((bool)lightIntensityCurve)
		{
			lightIntensityCurve.enabled = true;
		}
	}
}
