using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.SkinControllers;

[RequireComponent(typeof(Image))]
public class PanelSkinController : BaseSkinController
{
	public enum PanelType
	{
		Default,
		Header,
		Detail
	}

	public PanelType panelType;

	private Image image;

	protected new void Awake()
	{
		image = GetComponent<Image>();
		base.Awake();
	}

	protected override void OnSkinUI()
	{
		switch (panelType)
		{
		case PanelType.Default:
			skinData.mainPanelStyle.Apply(image);
			break;
		case PanelType.Header:
			skinData.headerPanelStyle.Apply(image);
			break;
		case PanelType.Detail:
			skinData.detailPanelStyle.Apply(image);
			break;
		}
	}
}
