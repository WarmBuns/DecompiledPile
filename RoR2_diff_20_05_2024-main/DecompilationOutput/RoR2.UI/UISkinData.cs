using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RoR2.UI;

[CreateAssetMenu(menuName = "RoR2/UISkinData")]
public class UISkinData : ScriptableObject
{
	[Serializable]
	public struct TextStyle
	{
		public TMP_FontAsset font;

		public float fontSize;

		public TextAlignmentOptions alignment;

		public Color color;

		public void Apply(TextMeshProUGUI label, bool useAlignment = true)
		{
			if (label.font != font && !(label is HGTextMeshProUGUI { useLanguageDefaultFont: not false }))
			{
				label.font = font;
			}
			if (label.fontSize != fontSize)
			{
				label.fontSize = fontSize;
			}
			if (label.color != color)
			{
				label.color = color;
			}
			if (useAlignment && label.alignment != alignment)
			{
				label.alignment = alignment;
			}
		}
	}

	[Serializable]
	public struct PanelStyle
	{
		public Material material;

		public Sprite sprite;

		public Color color;

		public void Apply(Image image)
		{
			image.material = material;
			image.sprite = sprite;
			image.color = color;
		}
	}

	[Serializable]
	public struct ButtonStyle
	{
		public Material material;

		public Sprite sprite;

		public ColorBlock colors;

		public TextStyle interactableTextStyle;

		public TextStyle disabledTextStyle;

		public float recommendedWidth;

		public float recommendedHeight;
	}

	[Serializable]
	public struct ScrollRectStyle
	{
		[FormerlySerializedAs("viewportPanelStyle")]
		public PanelStyle backgroundPanelStyle;

		public PanelStyle scrollbarBackgroundStyle;

		public ColorBlock scrollbarHandleColors;

		public Sprite scrollbarHandleImage;
	}

	[Header("Main Panel Style")]
	public PanelStyle mainPanelStyle;

	[Header("Header Style")]
	public PanelStyle headerPanelStyle;

	public TextStyle headerTextStyle;

	[Header("Detail Style")]
	public PanelStyle detailPanelStyle;

	public TextStyle detailTextStyle;

	[Header("Body Style")]
	public TextStyle bodyTextStyle;

	[Header("Button Style")]
	public ButtonStyle buttonStyle;

	[Header("Scroll Rect Style")]
	public ScrollRectStyle scrollRectStyle;
}
