using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2.UI;

[Obsolete("Use the options in HGButton instead.")]
public class SelectableDescriptionUpdater : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
	public LanguageTextMeshController languageTextMeshController;

	public string selectableDescriptionToken;

	public void OnPointerExit(PointerEventData eventData)
	{
		languageTextMeshController.token = "";
	}

	public void OnDeselect(BaseEventData eventData)
	{
		languageTextMeshController.token = "";
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		languageTextMeshController.token = selectableDescriptionToken;
	}

	public void OnSelect(BaseEventData eventData)
	{
		languageTextMeshController.token = selectableDescriptionToken;
	}
}
