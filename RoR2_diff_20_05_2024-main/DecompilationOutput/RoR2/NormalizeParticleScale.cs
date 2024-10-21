using UnityEngine;

namespace RoR2;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
public class NormalizeParticleScale : MonoBehaviour
{
	public bool normalizeWithSkinnedMeshRendererInstead;

	[Tooltip("if the mesh scale is below this value, don't use the renderer to normalize our scale")]
	public float skinnedMeshRendererMinScale;

	private ParticleSystem particleSystem;

	public void OnEnable()
	{
		UpdateParticleSystem();
	}

	public void UpdateParticleSystem()
	{
		if (!particleSystem)
		{
			particleSystem = GetComponent<ParticleSystem>();
		}
		ParticleSystem.MainModule main = particleSystem.main;
		ParticleSystem.MinMaxCurve startSize = main.startSize;
		Vector3 lossyScale = base.transform.lossyScale;
		float num = 1f;
		if (normalizeWithSkinnedMeshRendererInstead)
		{
			SkinnedMeshRenderer skinnedMeshRenderer = particleSystem.shape.skinnedMeshRenderer;
			if ((bool)skinnedMeshRenderer)
			{
				_ = skinnedMeshRenderer.transform.lossyScale;
				float num2 = Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
				if (num2 < skinnedMeshRendererMinScale)
				{
					num = num2;
				}
			}
		}
		else
		{
			num = Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
		}
		startSize.constantMin /= num;
		startSize.constantMax /= num;
		main.startSize = startSize;
	}
}
