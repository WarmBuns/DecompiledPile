using UnityEngine;
using UnityEngine.UI;

namespace RoR2;

public class AnimateImageAlpha : MonoBehaviour
{
	public AnimationCurve alphaCurve;

	public Image[] images;

	public float timeMax = 5f;

	public float delayBetweenElements;

	[HideInInspector]
	public float stopwatch;

	private void OnEnable()
	{
		stopwatch = 0f;
	}

	public void ResetStopwatch()
	{
		stopwatch = 0f;
	}

	private void LateUpdate()
	{
		stopwatch += Time.unscaledDeltaTime;
		int num = 0;
		Image[] array = images;
		foreach (Image obj in array)
		{
			num++;
			float a = alphaCurve.Evaluate((stopwatch + delayBetweenElements * (float)num) / timeMax);
			Color color = obj.color;
			obj.color = new Color(color.r, color.g, color.b, a);
		}
	}
}
