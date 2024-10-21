using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class ArtifactDisplayPanelController : MonoBehaviour
{
	[Tooltip("The panel object that this component controls. Should usually a child, as this component manages the enabled/disabled status of the designated gameobject.")]
	public GameObject panelObject;

	public RectTransform iconContainer;

	public GameObject iconPrefab;

	private UIElementAllocator<RawImage> iconAllocator;

	public void SetDisplayData<T>(ref T enabledArtifacts) where T : IEnumerator<ArtifactDef>
	{
		if (!panelObject)
		{
			return;
		}
		enabledArtifacts.Reset();
		int num = 0;
		while (enabledArtifacts.MoveNext())
		{
			num++;
		}
		panelObject.SetActive(num > 0);
		if (iconAllocator == null)
		{
			iconAllocator = new UIElementAllocator<RawImage>(iconContainer, iconPrefab);
		}
		iconAllocator.AllocateElements(num);
		int num2 = 0;
		enabledArtifacts.Reset();
		while (enabledArtifacts.MoveNext())
		{
			RawImage rawImage = iconAllocator.elements[num2++];
			ArtifactDef current = enabledArtifacts.Current;
			rawImage.texture = current.smallIconSelectedSprite.texture;
			TooltipProvider component = rawImage.GetComponent<TooltipProvider>();
			if ((bool)component)
			{
				component.titleToken = current.nameToken;
				component.titleColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Artifact);
				component.bodyToken = current.descriptionToken;
				component.bodyColor = Color.black;
				component.extraUIDisplayPrefab = current.extraUIDisplayPrefab;
			}
		}
	}
}
