using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class CustomScrollbar : MPScrollbar
{
	private Vector3 originalPosition;

	public bool scaleButtonOnHover = true;

	public bool showImageOnHover;

	public Image imageOnHover;

	public Image imageOnInteractable;

	private bool hovering;

	private float imageOnHoverAlphaVelocity;

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void Start()
	{
		base.Start();
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);
		switch (state)
		{
		case SelectionState.Normal:
			hovering = false;
			break;
		case SelectionState.Highlighted:
			Util.PlaySound("Play_UI_menuHover", RoR2Application.instance.gameObject);
			hovering = true;
			break;
		case SelectionState.Pressed:
			hovering = true;
			break;
		case SelectionState.Disabled:
			hovering = false;
			break;
		case SelectionState.Selected:
			break;
		}
	}

	public void OnClickCustom()
	{
		Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
	}

	private void LateUpdate()
	{
		if (Application.isPlaying)
		{
			if (showImageOnHover)
			{
				float target = (hovering ? 1f : 0f);
				Color color = imageOnHover.color;
				float a = Mathf.SmoothDamp(color.a, target, ref imageOnHoverAlphaVelocity, 0.03f, 100f, Time.unscaledDeltaTime);
				Color color2 = new Color(color.r, color.g, color.g, a);
				imageOnHover.color = color2;
			}
			if ((bool)imageOnInteractable)
			{
				imageOnInteractable.enabled = base.interactable;
			}
		}
	}
}
