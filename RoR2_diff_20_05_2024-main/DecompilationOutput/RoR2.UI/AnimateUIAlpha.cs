using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class AnimateUIAlpha : MonoBehaviour
{
	public AnimationCurve alphaCurve;

	public Image image;

	public RawImage rawImage;

	public SpriteRenderer spriteRenderer;

	public float timeMax = 5f;

	public bool destroyOnEnd;

	public bool loopOnEnd;

	public bool disableGameObjectOnEnd;

	[HideInInspector]
	public float time;

	private Color originalColor;

	private void Start()
	{
		if ((bool)image)
		{
			originalColor = image.color;
		}
		if ((bool)rawImage)
		{
			originalColor = rawImage.color;
		}
		else if ((bool)spriteRenderer)
		{
			originalColor = spriteRenderer.color;
		}
		UpdateAlphas(0f);
	}

	private void OnDisable()
	{
		time = 0f;
	}

	private void Update()
	{
		UpdateAlphas(Time.unscaledDeltaTime);
	}

	private void UpdateAlphas(float deltaTime)
	{
		time = Mathf.Min(timeMax, time + deltaTime);
		float num = alphaCurve.Evaluate(time / timeMax);
		Color color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * num);
		if ((bool)image)
		{
			image.color = color;
		}
		if ((bool)rawImage)
		{
			rawImage.color = color;
		}
		else if ((bool)spriteRenderer)
		{
			spriteRenderer.color = color;
		}
		if (loopOnEnd && time >= timeMax)
		{
			time -= timeMax;
		}
		if (destroyOnEnd && time >= timeMax)
		{
			Object.Destroy(base.gameObject);
		}
		if (disableGameObjectOnEnd && time >= timeMax)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
