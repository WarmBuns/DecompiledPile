using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

public class HGButton : MPButton
{
	private TextMeshProUGUI textMeshProUGui;

	private float stopwatch;

	private Color originalColor;

	[Tooltip("For automatic navigation: prefer objects with the same parent over objects elsewhere in the tree structure.")]
	public bool preferSiblings;

	public bool showImageOnHover;

	public Image imageOnHover;

	public Image imageOnInteractable;

	public bool updateTextOnHover;

	public LanguageTextMeshController hoverLanguageTextMeshController;

	public string hoverToken;

	private bool hovering;

	private Coroutine enableButtonCoroutine;

	private SelectionState previousState = SelectionState.Disabled;

	public string uiClickSoundOverride;

	private float buttonScaleVelocity;

	private float imageOnHoverAlphaVelocity;

	private float imageOnHoverScaleVelocity;

	protected override void Awake()
	{
		base.Awake();
		textMeshProUGui = GetComponent<TextMeshProUGUI>();
	}

	protected override void Start()
	{
		base.Start();
		base.onClick.AddListener(OnClickCustom);
		if (updateTextOnHover && !hoverLanguageTextMeshController)
		{
			Debug.LogErrorFormat("HGButton \"{0}\" is missing an object assigned to its .hoverLangaugeTextMeshController field.", Util.GetGameObjectHierarchyName(base.gameObject));
		}
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);
		if (previousState != state)
		{
			switch (state)
			{
			case SelectionState.Normal:
				hovering = false;
				break;
			case SelectionState.Highlighted:
			case SelectionState.Selected:
				if (!hovering)
				{
					Util.PlaySound("Play_UI_menuHover", RoR2Application.instance.gameObject);
				}
				hovering = true;
				break;
			case SelectionState.Pressed:
				hovering = true;
				break;
			case SelectionState.Disabled:
				hovering = false;
				break;
			}
			previousState = state;
		}
		originalColor = base.targetGraphic.color;
	}

	public void OnClickCustom()
	{
		Util.PlaySound((!string.IsNullOrEmpty(uiClickSoundOverride)) ? uiClickSoundOverride : "Play_UI_menuClick", RoR2Application.instance.gameObject);
	}

	private void LateUpdate()
	{
		stopwatch += Time.deltaTime;
		if (Application.isPlaying)
		{
			if (showImageOnHover)
			{
				float target = (hovering ? 1f : 0f);
				float target2 = (hovering ? 1f : 1.1f);
				Color color = imageOnHover.color;
				float x = imageOnHover.transform.localScale.x;
				float a = Mathf.SmoothDamp(color.a, target, ref imageOnHoverAlphaVelocity, 0.03f * base.colors.fadeDuration, float.PositiveInfinity, Time.unscaledDeltaTime);
				float num = Mathf.SmoothDamp(x, target2, ref imageOnHoverScaleVelocity, 0.03f, float.PositiveInfinity, Time.unscaledDeltaTime);
				Color color2 = new Color(color.r, color.g, color.g, a);
				Vector3 localScale = new Vector3(num, num, num);
				imageOnHover.color = color2;
				imageOnHover.transform.localScale = localScale;
			}
			if ((bool)imageOnInteractable)
			{
				imageOnInteractable.enabled = base.interactable;
			}
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		if (updateTextOnHover && (bool)hoverLanguageTextMeshController)
		{
			hoverLanguageTextMeshController.token = hoverToken;
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		if (updateTextOnHover && (bool)hoverLanguageTextMeshController)
		{
			hoverLanguageTextMeshController.token = "";
		}
		base.OnPointerExit(eventData);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		if (base.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad)
		{
			hovering = true;
		}
		if (updateTextOnHover && (bool)hoverLanguageTextMeshController)
		{
			hoverLanguageTextMeshController.token = hoverToken;
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		if (base.eventSystem != null && base.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad)
		{
			hovering = false;
		}
	}

	public override Selectable FindSelectableOnLeft()
	{
		if (!preferSiblings)
		{
			return base.FindSelectableOnLeft();
		}
		if (base.navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
		{
			return base.navigation.selectOnLeft;
		}
		if ((base.navigation.mode & UnityEngine.UI.Navigation.Mode.Horizontal) != 0)
		{
			return FindSelectablePreferSibling(base.transform.rotation * Vector3.left);
		}
		return null;
	}

	public override Selectable FindSelectableOnRight()
	{
		if (!preferSiblings)
		{
			return base.FindSelectableOnRight();
		}
		if (base.navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
		{
			return base.navigation.selectOnRight;
		}
		if ((base.navigation.mode & UnityEngine.UI.Navigation.Mode.Horizontal) != 0)
		{
			return FindSelectablePreferSibling(base.transform.rotation * Vector3.right);
		}
		return null;
	}

	private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
	{
		if (rect == null)
		{
			return Vector3.zero;
		}
		if (dir != Vector2.zero)
		{
			dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
		}
		dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
		return dir;
	}

	private Selectable FindSelectablePreferSibling(Vector3 dir)
	{
		dir = dir.normalized;
		Vector3 vector = Quaternion.Inverse(base.transform.rotation) * dir;
		Vector3 vector2 = base.transform.TransformPoint(GetPointOnRectEdge(base.transform as RectTransform, vector));
		float num = float.NegativeInfinity;
		float num2 = float.NegativeInfinity;
		float num3 = 0f;
		UnityEngine.UI.Navigation.Mode mode = base.navigation.mode;
		bool flag = base.navigation.wrapAround && (mode == UnityEngine.UI.Navigation.Mode.Vertical || mode == UnityEngine.UI.Navigation.Mode.Horizontal);
		Selectable selectable = null;
		Selectable result = null;
		for (int i = 0; i < Selectable.s_SelectableCount; i++)
		{
			Selectable selectable2 = Selectable.s_Selectables[i];
			if (selectable2 == this || !selectable2.IsInteractable() || selectable2.navigation.mode == UnityEngine.UI.Navigation.Mode.None)
			{
				continue;
			}
			bool flag2 = base.transform.parent == selectable2.transform.parent;
			RectTransform rectTransform = selectable2.transform as RectTransform;
			Vector3 position = ((rectTransform != null) ? ((Vector3)rectTransform.rect.center) : Vector3.zero);
			Vector3 rhs = selectable2.transform.TransformPoint(position) - vector2;
			float num4 = Vector3.Dot(dir, rhs);
			if (flag && num4 < 0f)
			{
				num3 = (0f - num4) * rhs.sqrMagnitude;
				if (flag2)
				{
					num3 *= 2f;
				}
				if (num3 > num2)
				{
					num2 = num3;
					result = selectable2;
				}
			}
			else if (!(num4 <= 0f))
			{
				num3 = num4 / rhs.sqrMagnitude;
				if (flag2)
				{
					num3 *= 2f;
				}
				if (num3 > num)
				{
					num = num3;
					selectable = selectable2;
				}
			}
		}
		if (flag && null == selectable)
		{
			return result;
		}
		return selectable;
	}
}
