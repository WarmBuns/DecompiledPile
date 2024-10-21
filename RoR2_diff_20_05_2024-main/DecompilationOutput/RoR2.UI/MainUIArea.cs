using UnityEngine;

namespace RoR2.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class MainUIArea : MonoBehaviour
{
	private RectTransform rectTransform;

	private RectTransform parentRectTransform;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		parentRectTransform = rectTransform.parent.GetComponent<RectTransform>();
	}

	private void Update()
	{
		Rect rect = parentRectTransform.rect;
		float num = rect.width * 0.05f;
		float num2 = rect.height * 0.05f;
		rectTransform.offsetMin = new Vector2(num, num2);
		rectTransform.offsetMax = new Vector2(0f - num, 0f - num2);
	}
}
