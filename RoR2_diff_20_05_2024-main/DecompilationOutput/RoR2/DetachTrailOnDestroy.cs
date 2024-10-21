using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

public class DetachTrailOnDestroy : MonoBehaviour
{
	[FormerlySerializedAs("trail")]
	public TrailRenderer[] targetTrailRenderers;

	[SerializeField]
	[Tooltip("Use this to disable the script from functioning.")]
	private bool IgnoreDestroy;

	private void OnDestroy()
	{
		if (IgnoreDestroy)
		{
			return;
		}
		for (int i = 0; i < targetTrailRenderers.Length; i++)
		{
			TrailRenderer trailRenderer = targetTrailRenderers[i];
			if ((bool)trailRenderer)
			{
				trailRenderer.transform.SetParent(null);
				trailRenderer.autodestruct = true;
			}
		}
		targetTrailRenderers = null;
	}
}
