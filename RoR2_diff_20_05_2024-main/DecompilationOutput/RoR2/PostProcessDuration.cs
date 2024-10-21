using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2;

public class PostProcessDuration : MonoBehaviour
{
	public PostProcessVolume ppVolume;

	public AnimationCurve ppWeightCurve;

	public float maxDuration;

	public bool destroyOnEnd;

	public bool useUnscaledTime;

	private float stopwatch;

	private void Update()
	{
		stopwatch += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
		UpdatePostProccess();
	}

	private void Awake()
	{
		UpdatePostProccess();
	}

	private void OnEnable()
	{
		stopwatch = 0f;
	}

	private void UpdatePostProccess()
	{
		float num = Mathf.Clamp01(stopwatch / maxDuration);
		ppVolume.weight = ppWeightCurve.Evaluate(num);
		PostProcessVolume.DispatchVolumeSettingsChangedEvent();
		if (num == 1f && destroyOnEnd)
		{
			Object.Destroy(ppVolume.gameObject);
		}
	}

	private void OnValidate()
	{
		if (maxDuration <= Mathf.Epsilon)
		{
			Debug.LogWarningFormat("{0} has PP of time zero!", base.gameObject);
		}
	}
}
