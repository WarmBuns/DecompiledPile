using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Animator))]
public class ForcePose : MonoBehaviour
{
	[Tooltip("The animation clip to force.")]
	public AnimationClip clip;

	[Range(0f, 1f)]
	[Tooltip("The moment in the cycle to force.")]
	public float cycle;

	private void Start()
	{
	}

	private void Update()
	{
		if ((bool)clip)
		{
			clip.SampleAnimation(base.gameObject, cycle * clip.length);
		}
	}
}
