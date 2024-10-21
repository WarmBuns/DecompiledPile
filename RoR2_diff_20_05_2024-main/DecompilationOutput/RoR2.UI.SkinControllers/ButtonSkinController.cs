using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.SkinControllers;

[RequireComponent(typeof(Button))]
public class ButtonSkinController : BaseSkinController
{
	private static readonly List<ButtonSkinController> instancesList = new List<ButtonSkinController>();

	private Button button;

	public bool useRecommendedButtonWidth = true;

	public bool useRecommendedButtonHeight = true;

	public bool useRecommendedImage = true;

	public bool useRecommendedMaterial = true;

	public bool useRecommendedAlignment = true;

	public bool useRecommendedLabel = true;

	private LayoutElement layoutElement;

	private void CacheComponents()
	{
		button = GetComponent<Button>();
		layoutElement = GetComponent<LayoutElement>();
	}

	protected new void Awake()
	{
		CacheComponents();
		base.Awake();
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RoR2Application.onUpdate += StaticUpdate;
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private static void StaticUpdate()
	{
		foreach (ButtonSkinController instances in instancesList)
		{
			instances.UpdateLabelStyle(ref instances.skinData.buttonStyle);
		}
	}

	private void UpdateLabelStyle(ref UISkinData.ButtonStyle buttonStyle)
	{
		if (!useRecommendedLabel)
		{
			return;
		}
		TextMeshProUGUI componentInChildren = button.GetComponentInChildren<TextMeshProUGUI>();
		if ((bool)componentInChildren)
		{
			if (button.interactable)
			{
				buttonStyle.interactableTextStyle.Apply(componentInChildren, useRecommendedAlignment);
			}
			else
			{
				buttonStyle.disabledTextStyle.Apply(componentInChildren, useRecommendedAlignment);
			}
		}
	}

	protected override void OnSkinUI()
	{
		ApplyButtonStyle(ref skinData.buttonStyle);
	}

	private void ApplyButtonStyle(ref UISkinData.ButtonStyle buttonStyle)
	{
		if (useRecommendedMaterial)
		{
			button.image.material = buttonStyle.material;
		}
		button.colors = buttonStyle.colors;
		if (useRecommendedImage)
		{
			button.image.sprite = buttonStyle.sprite;
		}
		if (useRecommendedButtonWidth)
		{
			((RectTransform)base.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, buttonStyle.recommendedWidth);
		}
		if (useRecommendedButtonHeight)
		{
			((RectTransform)base.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonStyle.recommendedHeight);
		}
		if ((bool)layoutElement)
		{
			if (useRecommendedButtonWidth)
			{
				layoutElement.preferredWidth = buttonStyle.recommendedWidth;
			}
			if (useRecommendedButtonHeight)
			{
				layoutElement.preferredHeight = buttonStyle.recommendedHeight;
			}
		}
		UpdateLabelStyle(ref buttonStyle);
	}

	private void OnValidate()
	{
		CacheComponents();
	}
}
