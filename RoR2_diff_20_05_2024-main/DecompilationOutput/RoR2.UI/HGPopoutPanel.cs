using System.Collections.Generic;
using UnityEngine;

namespace RoR2.UI;

public class HGPopoutPanel : MonoBehaviour
{
	public int popoutPanelLayer;

	public bool usesAdditionalInfo;

	[Header("Optional Referenced Components")]
	public RectTransform popoutPanelContentContainer;

	public LanguageTextMeshController popoutPanelTitleText;

	public LanguageTextMeshController popoutPanelSubtitleText;

	public LanguageTextMeshController popoutPanelDescriptionText;

	public GameObject popoutAdditionalInfoContainer;

	public static List<HGPopoutPanel> instances = new List<HGPopoutPanel>();

	private void OnEnable()
	{
		for (int i = 0; i < instances.Count; i++)
		{
			HGPopoutPanel hGPopoutPanel = instances[i];
			if (popoutPanelLayer == hGPopoutPanel.popoutPanelLayer)
			{
				hGPopoutPanel.gameObject.SetActive(value: false);
			}
		}
		instances.Add(this);
		if (usesAdditionalInfo)
		{
			popoutAdditionalInfoContainer.SetActive(value: true);
		}
	}

	private void OnDisable()
	{
		instances.Remove(this);
	}
}
