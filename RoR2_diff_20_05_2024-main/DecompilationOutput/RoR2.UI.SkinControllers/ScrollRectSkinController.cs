using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.SkinControllers;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectSkinController : BaseSkinController
{
	private ScrollRect scrollRect;

	protected new void Awake()
	{
		scrollRect = GetComponent<ScrollRect>();
		base.Awake();
	}

	protected override void OnSkinUI()
	{
		Image component = GetComponent<Image>();
		if ((bool)component)
		{
			skinData.scrollRectStyle.backgroundPanelStyle.Apply(component);
		}
		if ((bool)scrollRect.verticalScrollbar)
		{
			SkinScrollbar(scrollRect.verticalScrollbar);
		}
		if ((bool)scrollRect.horizontalScrollbar)
		{
			SkinScrollbar(scrollRect.horizontalScrollbar);
		}
	}

	private void SkinScrollbar(Scrollbar scrollbar)
	{
		skinData.scrollRectStyle.scrollbarBackgroundStyle.Apply(scrollbar.GetComponent<Image>());
		scrollbar.colors = skinData.scrollRectStyle.scrollbarHandleColors;
		scrollbar.handleRect.GetComponent<Image>().sprite = skinData.scrollRectStyle.scrollbarHandleImage;
	}
}
