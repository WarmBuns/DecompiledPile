using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class ContextManager : MonoBehaviour
{
	public TextMeshProUGUI glyphTMP;

	public TextMeshProUGUI descriptionTMP;

	public TextMeshProUGUI inspectDescriptionTMP;

	public GameObject contextDisplay;

	public TextMeshProUGUI inspectGlyphTMP;

	public GameObject inspectDisplay;

	public HUD hud;

	private MPEventSystemLocator eventSystemLocator;

	private string glyphString;

	private GameObject cachedInteractionTarget;

	private bool lastContextEnabled = true;

	private string basicInspectString;

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		basicInspectString = Language.GetString("ACTION_INSPECT");
		InputBindingDisplayController.onBindingsChanged = (Action)Delegate.Combine(InputBindingDisplayController.onBindingsChanged, new Action(UpdateGlyphString));
	}

	private void OnDestroy()
	{
		InputBindingDisplayController.onBindingsChanged = (Action)Delegate.Remove(InputBindingDisplayController.onBindingsChanged, new Action(UpdateGlyphString));
	}

	private void Start()
	{
		UpdateGlyphString();
		Update();
	}

	private void UpdateGlyphString()
	{
		glyphString = string.Format(CultureInfo.InvariantCulture, "<style=cKeyBinding>{0}</style>", Glyphs.GetGlyphString(eventSystemLocator, "Interact"));
		glyphTMP.text = glyphString;
	}

	private void Update()
	{
		string text = "";
		string text2 = "";
		string text3 = "";
		string text4 = "";
		bool flag = false;
		bool active = false;
		if ((bool)hud && (bool)hud.targetBodyObject)
		{
			InteractionDriver component = hud.targetBodyObject.GetComponent<InteractionDriver>();
			if ((bool)component)
			{
				GameObject currentInteractable = component.currentInteractable;
				if ((bool)currentInteractable)
				{
					PlayerCharacterMasterController component2 = hud.targetMaster.GetComponent<PlayerCharacterMasterController>();
					if ((bool)component2 && (bool)component2.networkUser && component2.networkUser.localUser != null)
					{
						IInteractable component3 = currentInteractable.GetComponent<IInteractable>();
						if (component3 != null && ((MonoBehaviour)component3).isActiveAndEnabled)
						{
							string text5 = ((component3.GetInteractability(component.interactor) == Interactability.Available) ? component3.GetContextString(component.interactor) : null);
							if (text5 != null)
							{
								text3 = text5;
								text = string.Format(CultureInfo.InvariantCulture, "<style=cKeyBinding>{0}</style>", Glyphs.GetGlyphString(eventSystemLocator, "Interact"));
								flag = true;
							}
							if (hud.localUserViewer.userProfile.useInspectFeature)
							{
								IInspectable component4 = currentInteractable.GetComponent<IInspectable>();
								if (component4 != null)
								{
									IInspectInfoProvider inspectInfoProvider = component4.GetInspectInfoProvider();
									if (inspectInfoProvider != null)
									{
										text4 = basicInspectString;
										text2 = string.Format(CultureInfo.InvariantCulture, "<style=cKeyBinding>{0}</style>", Glyphs.GetGlyphString(eventSystemLocator, "info"));
										active = true;
										if (inspectInfoProvider is IHasInspectHintOverride hasInspectHintOverride && hasInspectHintOverride.GetInspectHintOverride(out var hintOverride))
										{
											text4 = hintOverride;
										}
									}
								}
							}
						}
					}
				}
			}
		}
		if (flag || (!flag && lastContextEnabled))
		{
			glyphTMP.text = text;
			descriptionTMP.text = text3;
			contextDisplay.SetActive(flag);
		}
		inspectGlyphTMP.text = text2;
		inspectDescriptionTMP.text = text4;
		inspectDisplay.SetActive(active);
	}
}
