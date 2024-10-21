using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(Image))]
public class StageFadeTransitionController : MonoBehaviour
{
	private Image fadeImage;

	private float startEngineTime;

	private const float transitionDuration = 0.5f;

	private void Awake()
	{
		fadeImage = GetComponent<Image>();
		Color color = fadeImage.color;
		color.a = 1f;
		fadeImage.color = color;
	}

	private void Start()
	{
		Color color = fadeImage.color;
		color.a = 1f;
		fadeImage.color = color;
		fadeImage.CrossFadeColor(Color.black, 0.5f, ignoreTimeScale: false, useAlpha: true);
		startEngineTime = Time.time;
	}

	private void Update()
	{
		if ((bool)Stage.instance)
		{
			Run.FixedTimeStamp stageAdvanceTime = Stage.instance.stageAdvanceTime;
			float num = Time.time - startEngineTime;
			float a = 0f;
			float b = 0f;
			if (num < 0.5f)
			{
				a = 1f - Mathf.Clamp01((Time.time - startEngineTime) / 0.5f);
			}
			if (!stageAdvanceTime.isInfinity)
			{
				float num2 = Stage.instance.stageAdvanceTime - 0.25f - Run.FixedTimeStamp.now;
				b = 1f - Mathf.Clamp01(num2 / 0.5f);
			}
			Color color = fadeImage.color;
			color.a = Mathf.Max(a, b);
			fadeImage.color = color;
		}
	}
}
