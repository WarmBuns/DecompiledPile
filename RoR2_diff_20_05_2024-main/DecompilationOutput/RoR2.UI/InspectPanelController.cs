using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class InspectPanelController : MonoBehaviour
{
	[SerializeField]
	private GameObject InspectPanel;

	[SerializeField]
	private Image InspectVisual;

	[SerializeField]
	private LanguageTextMeshController InspectTitle;

	[SerializeField]
	private TMP_Text InspectTitleText;

	[SerializeField]
	private LanguageTextMeshController InspectDescription;

	[SerializeField]
	private LanguageTextMeshController InspectFlavor;

	[SerializeField]
	private GameObject Sidecar;

	[SerializeField]
	private Image SidecarInspectImage;

	[SerializeField]
	private List<ItemDef> TestItems;

	[SerializeField]
	private bool isAutopauseCandidate;

	public InspectDef MissingInspectDefFallback;

	public InspectDef UnknownItemDefOverride;

	private InspectInfo SidecarStoredInfo;

	private MPEventSystem eventSystem;

	private void Awake()
	{
		if ((bool)InspectPanel)
		{
			InspectPanel.SetActive(value: false);
		}
		if ((bool)Sidecar)
		{
			Sidecar.SetActive(value: false);
		}
		MPEventSystemLocator component = GetComponent<MPEventSystemLocator>();
		eventSystem = component.eventSystem;
	}

	public void Show(InspectInfo ToShow, bool WithSidecar = false)
	{
		if (eventSystem == null)
		{
			MPEventSystemLocator component = GetComponent<MPEventSystemLocator>();
			eventSystem = component.eventSystem;
		}
		InspectPanel.SetActive(value: false);
		if (ToShow == null)
		{
			ToShow = MissingInspectDefFallback;
		}
		if (ToShow.isConsumedItem)
		{
			ToShow.MarkForceShowInfo();
		}
		if (ToShow.isDynamicInspectInfo && !ToShow.ForceShowInfo && !eventSystem.localUser.userProfile.HasDiscoveredPickup(ToShow.dynamicInspectPickupIndex))
		{
			ToShow.TitleToken = UnknownItemDefOverride.Info.TitleToken;
			ToShow.DescriptionToken = UnknownItemDefOverride.Info.DescriptionToken;
			ToShow.FlavorToken = UnknownItemDefOverride.Info.FlavorToken;
		}
		InspectVisual.sprite = (ToShow.Visual ? ToShow.Visual : MissingInspectDefFallback.Info.Visual);
		InspectTitle.token = ((ToShow.TitleToken.Length > 0) ? ToShow.TitleToken : MissingInspectDefFallback.Info.TitleToken);
		InspectTitleText.color = ToShow.TitleColor;
		if (InspectDescription != null)
		{
			InspectDescription.token = ((ToShow.DescriptionToken.Length > 0) ? ToShow.DescriptionToken : MissingInspectDefFallback.Info.DescriptionToken);
		}
		if (InspectFlavor != null)
		{
			InspectFlavor.token = ((ToShow.FlavorToken.Length > 0) ? ToShow.FlavorToken : MissingInspectDefFallback.Info.FlavorToken);
		}
		InspectPanel.SetActive(value: true);
		if ((bool)Sidecar && (bool)SidecarInspectImage && WithSidecar)
		{
			SidecarStoredInfo = ToShow;
			Sidecar.SetActive(value: true);
			SidecarInspectImage.sprite = ToShow.Visual;
			eventSystem.SetSelectedObject(Sidecar.GetComponentInChildren<HGButton>().gameObject);
		}
		bool useInspectAutopause = eventSystem.localUser.userProfile.useInspectAutopause;
		if (isAutopauseCandidate && useInspectAutopause && RoR2Application.isInSinglePlayer)
		{
			PauseStopController.instance?.Pause(shouldPause: true);
		}
	}

	public void DisplaySidecarInfo()
	{
		Show(SidecarStoredInfo, WithSidecar: true);
	}

	private void OnDisable()
	{
		OnCloseButtonClicked();
	}

	public void ForceEnableInspectPanel()
	{
		if (InspectPanel != null)
		{
			InspectPanel.SetActive(value: true);
		}
	}

	public void OnCloseButtonClicked()
	{
		InspectPanel.SetActive(value: false);
	}
}
