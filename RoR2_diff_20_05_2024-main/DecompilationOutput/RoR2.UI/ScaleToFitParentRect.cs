using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ScaleToFitParentRect : MonoBehaviour, ILayoutSelfController, ILayoutController
{
	private RectTransform rectTransform;

	private RectTransform parentRectTransform;

	public bool fitToWidth = true;

	public bool fitToHeight = true;

	public bool stretchUnfittedAxis;

	public Vector2 referenceSize = Vector2.zero;

	private Vector2 parentSize = Vector2.zero;

	private Vector2 postScaleStretchSize = Vector2.zero;

	private float desiredScale = 1f;

	public bool autoReferenceSize = true;

	private bool inApply;

	private void CacheComponents()
	{
		rectTransform = (RectTransform)base.transform;
	}

	private void Awake()
	{
		CacheComponents();
		parentRectTransform = rectTransform.parent as RectTransform;
	}

	private void Start()
	{
	}

	private void OnTransformParentChanged()
	{
		parentRectTransform = rectTransform.parent as RectTransform;
	}

	private void RecalculateScale()
	{
		if ((bool)parentRectTransform)
		{
			parentSize = parentRectTransform.rect.size;
			float a = parentSize.x / referenceSize.x;
			float b = parentSize.y / referenceSize.y;
			if (fitToWidth && fitToHeight)
			{
				desiredScale = Mathf.Min(a, b);
			}
			else if (fitToWidth)
			{
				desiredScale = a;
			}
			else if (fitToHeight)
			{
				desiredScale = b;
			}
			else
			{
				desiredScale = 1f;
			}
			postScaleStretchSize = parentSize / desiredScale;
			if (float.IsNaN(postScaleStretchSize.x))
			{
				postScaleStretchSize.x = 1f;
			}
			if (float.IsNaN(postScaleStretchSize.y))
			{
				postScaleStretchSize.y = 1f;
			}
		}
	}

	private void OnRectTransformDimensionsChange()
	{
		if (!inApply)
		{
			Apply();
		}
	}

	public void SetLayoutHorizontal()
	{
		RecalculateScale();
		Vector3 localScale = rectTransform.localScale;
		if (localScale.x != desiredScale)
		{
			localScale.x = desiredScale;
			rectTransform.localScale = localScale;
		}
		if (stretchUnfittedAxis && !fitToWidth && rectTransform.rect.width != postScaleStretchSize.x)
		{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, postScaleStretchSize.x);
		}
	}

	public void SetLayoutVertical()
	{
		RecalculateScale();
		Vector3 localScale = rectTransform.localScale;
		if (localScale.y != desiredScale)
		{
			localScale.y = desiredScale;
			rectTransform.localScale = localScale;
		}
		if (stretchUnfittedAxis && !fitToHeight && rectTransform.rect.height != postScaleStretchSize.y)
		{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, postScaleStretchSize.y);
		}
	}

	public void Apply()
	{
		if (!base.enabled || (object)rectTransform == null)
		{
			return;
		}
		RecalculateScale();
		inApply = true;
		Vector3 localScale = rectTransform.localScale;
		if (localScale.x != desiredScale && desiredScale != 0f)
		{
			localScale.x = desiredScale;
			localScale.y = desiredScale;
			rectTransform.localScale = localScale;
		}
		if (stretchUnfittedAxis)
		{
			Rect rect = rectTransform.rect;
			if (!fitToWidth && rect.width != postScaleStretchSize.x)
			{
				rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, postScaleStretchSize.x);
			}
			if (!fitToWidth && rect.height != postScaleStretchSize.y)
			{
				rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, postScaleStretchSize.y);
			}
		}
		inApply = false;
	}

	private void OnValidate()
	{
		CacheComponents();
		parentRectTransform = rectTransform.parent as RectTransform;
		if (autoReferenceSize)
		{
			referenceSize = rectTransform.rect.size;
		}
		Apply();
	}
}
