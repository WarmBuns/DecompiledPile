using UnityEngine;

namespace RoR2.UI;

public class UIJuice : MonoBehaviour
{
	[Header("Transition Settings")]
	public CanvasGroup canvasGroup;

	public RectTransform panningRect;

	public float transitionDuration;

	public float panningMagnitude;

	public bool destroyOnEndOfTransition;

	public bool disableAndResetOnEndOfTransition;

	private float transitionStopwatch;

	private float transitionEndAlpha;

	private float transitionStartAlpha;

	private float originalAlpha;

	private Vector2 transitionStartPosition;

	private Vector2 transitionEndPosition;

	private Vector2 originalPosition;

	private Vector2 transitionStartSize;

	private Vector2 transitionEndSize;

	private Vector3 originalSize;

	private bool wasTransition;

	private bool hasInitialized;

	private void Awake()
	{
		InitializeFirstTimeInfo();
	}

	private void Update()
	{
		transitionStopwatch = Mathf.Min(transitionStopwatch + Time.unscaledDeltaTime, transitionDuration);
		ProcessTransition();
	}

	private void ProcessTransition()
	{
		InitializeFirstTimeInfo();
		bool flag = transitionStopwatch < transitionDuration;
		if (flag || flag != wasTransition)
		{
			if (flag)
			{
				AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, transitionStartAlpha, 1f, transitionEndAlpha);
				if ((bool)canvasGroup)
				{
					canvasGroup.alpha = animationCurve.Evaluate(transitionStopwatch / transitionDuration);
				}
				AnimationCurve animationCurve2 = new AnimationCurve();
				Keyframe key = new Keyframe(0f, 0f, 3f, 3f);
				Keyframe key2 = new Keyframe(1f, 1f, 0f, 0f);
				animationCurve2.AddKey(key);
				animationCurve2.AddKey(key2);
				Vector2 anchoredPosition = Vector2.Lerp(transitionStartPosition, transitionEndPosition, animationCurve2.Evaluate(transitionStopwatch / transitionDuration));
				Vector2 sizeDelta = Vector2.Lerp(transitionStartSize, transitionEndSize, animationCurve2.Evaluate(transitionStopwatch / transitionDuration));
				if ((bool)panningRect)
				{
					panningRect.anchoredPosition = anchoredPosition;
					panningRect.sizeDelta = sizeDelta;
				}
			}
			else
			{
				if ((bool)canvasGroup)
				{
					canvasGroup.alpha = transitionEndAlpha;
				}
				if ((bool)panningRect)
				{
					panningRect.anchoredPosition = transitionEndPosition;
					panningRect.sizeDelta = transitionEndSize;
				}
				if (destroyOnEndOfTransition)
				{
					Object.Destroy(base.gameObject);
				}
				else if (disableAndResetOnEndOfTransition)
				{
					InitializeFirstTimeInfo();
					transitionStopwatch = 0f;
					base.gameObject.SetActive(value: false);
				}
			}
		}
		wasTransition = flag;
	}

	public void TransitionScaleUpWidth()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartSize = new Vector2(0f, transitionEndSize.y * 0.8f);
			transitionEndSize = originalSize;
		}
		BeginTransition();
	}

	public void TransitionPanFromLeft()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = new Vector2(-1f, 0f) * panningMagnitude;
			transitionEndPosition = originalPosition;
		}
		BeginTransition();
	}

	public void TransitionPanToLeft()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = originalPosition;
			transitionEndPosition = new Vector2(-1f, 0f) * panningMagnitude;
		}
		BeginTransition();
	}

	public void TransitionPanFromRight()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = new Vector2(1f, 0f) * panningMagnitude;
			transitionEndPosition = originalPosition;
		}
		BeginTransition();
	}

	public void TransitionPanToRight()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = originalPosition;
			transitionEndPosition = new Vector2(1f, 0f) * panningMagnitude;
		}
		BeginTransition();
	}

	public void TransitionPanFromTop()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = new Vector2(0f, 1f) * panningMagnitude;
			transitionEndPosition = originalPosition;
		}
		BeginTransition();
	}

	public void TransitionPanToTop()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = originalPosition;
			transitionEndPosition = new Vector2(0f, 1f) * panningMagnitude;
		}
		BeginTransition();
	}

	public void TransitionPanFromBottom()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = new Vector2(0f, -1f) * panningMagnitude;
			transitionEndPosition = originalPosition;
		}
		BeginTransition();
	}

	public void TransitionPanToBottom()
	{
		InitializeFirstTimeInfo();
		if ((bool)panningRect)
		{
			transitionStartPosition = originalPosition;
			transitionEndPosition = new Vector2(0f, -1f) * panningMagnitude;
		}
		BeginTransition();
	}

	public void TransitionAlphaFadeIn()
	{
		InitializeFirstTimeInfo();
		transitionStartAlpha = 0f;
		transitionEndAlpha = originalAlpha;
		BeginTransition();
	}

	public void TransitionAlphaFadeOut()
	{
		InitializeFirstTimeInfo();
		transitionStartAlpha = originalAlpha;
		transitionEndAlpha = 0f;
		BeginTransition();
	}

	public void DestroyOnEndOfTransition(bool set)
	{
		destroyOnEndOfTransition = set;
	}

	public void DisableAndResetOnEndOfTransition(bool set)
	{
		disableAndResetOnEndOfTransition = set;
	}

	private void BeginTransition()
	{
		transitionStopwatch = 0f;
		ProcessTransition();
	}

	private void InitializeFirstTimeInfo()
	{
		if (!hasInitialized)
		{
			if ((bool)panningRect)
			{
				originalPosition = panningRect.anchoredPosition;
				originalSize = panningRect.sizeDelta;
			}
			if ((bool)canvasGroup)
			{
				originalAlpha = canvasGroup.alpha;
				transitionEndAlpha = originalAlpha;
				transitionStartAlpha = originalAlpha;
			}
			hasInitialized = true;
		}
	}
}
