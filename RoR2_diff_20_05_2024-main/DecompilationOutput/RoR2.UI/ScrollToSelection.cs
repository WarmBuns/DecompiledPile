using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
[RequireComponent(typeof(ScrollRect))]
public class ScrollToSelection : MonoBehaviour
{
	private ScrollRect scrollRect;

	private MPEventSystemLocator eventSystemLocator;

	private Vector3[] targetWorldCorners = new Vector3[4];

	private Vector3[] viewPortWorldCorners = new Vector3[4];

	private GameObject lastSelectedObject;

	private EventSystem eventSystem => eventSystemLocator.eventSystem;

	private void Awake()
	{
		scrollRect = GetComponent<ScrollRect>();
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	private void Update()
	{
		GameObject gameObject = (eventSystem ? eventSystem.currentSelectedGameObject : null);
		if (lastSelectedObject != gameObject)
		{
			if ((bool)gameObject && gameObject.transform.IsChildOf(base.transform))
			{
				ScrollToRect((RectTransform)gameObject.transform);
			}
			lastSelectedObject = gameObject;
		}
	}

	private void ScrollToRect(RectTransform targetRectTransform)
	{
		targetRectTransform.GetWorldCorners(targetWorldCorners);
		((RectTransform)base.transform).GetWorldCorners(viewPortWorldCorners);
		if (scrollRect.vertical && (bool)scrollRect.verticalScrollbar)
		{
			float y = targetWorldCorners[1].y;
			float y2 = targetWorldCorners[0].y;
			float y3 = viewPortWorldCorners[1].y;
			float y4 = viewPortWorldCorners[0].y;
			float num = y - y3;
			float num2 = y2 - y4;
			float num3 = y3 - y4;
			if (num > 0f)
			{
				scrollRect.verticalScrollbar.value += num / num3;
			}
			if (num2 < 0f)
			{
				scrollRect.verticalScrollbar.value += num2 / num3;
			}
		}
		if (scrollRect.horizontal && (bool)scrollRect.horizontalScrollbar)
		{
			float y5 = targetWorldCorners[2].y;
			float y6 = targetWorldCorners[0].y;
			float y7 = viewPortWorldCorners[2].y;
			float y8 = viewPortWorldCorners[0].y;
			float num4 = y5 - y7;
			float num5 = y6 - y8;
			float num6 = y7 - y8;
			if (num4 > 0f)
			{
				scrollRect.horizontalScrollbar.value += num4 / num6;
			}
			if (num5 < 0f)
			{
				scrollRect.horizontalScrollbar.value += num5 / num6;
			}
		}
	}
}
