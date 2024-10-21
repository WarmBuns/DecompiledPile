using TMPro;
using UnityEngine;

namespace RoR2.UI.SkinControllers;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LabelSkinController : BaseSkinController
{
	public enum LabelType
	{
		Default,
		Header,
		Detail
	}

	public LabelType labelType;

	public bool useRecommendedAlignment = true;

	private TextMeshProUGUI label;

	protected new void Awake()
	{
		label = GetComponent<TextMeshProUGUI>();
		base.Awake();
	}

	protected override void OnSkinUI()
	{
		switch (labelType)
		{
		case LabelType.Default:
			skinData.bodyTextStyle.Apply(label, useRecommendedAlignment);
			break;
		case LabelType.Header:
			skinData.headerTextStyle.Apply(label, useRecommendedAlignment);
			break;
		case LabelType.Detail:
			skinData.detailTextStyle.Apply(label, useRecommendedAlignment);
			break;
		}
	}
}
