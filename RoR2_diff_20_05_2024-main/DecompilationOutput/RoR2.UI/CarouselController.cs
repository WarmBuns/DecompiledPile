using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RoR2.UI;

public class CarouselController : BaseSettingsControl
{
	[Serializable]
	public struct Choice
	{
		public string suboptionDisplayToken;

		public string convarValue;

		public Sprite customSprite;
	}

	public GameObject leftArrowButton;

	public GameObject rightArrowButton;

	public Image optionalImage;

	public TextMeshProUGUI optionalText;

	[FormerlySerializedAs("choices")]
	[NotNull]
	[SerializeField]
	private Choice[] _choices = Array.Empty<Choice>();

	public bool forceValidChoice;

	[Tooltip("If true, this setting will control+reflect the given subsectionHelper's child settings")]
	public bool controlsSubsection;

	public SettingSubsectionHelper subsectionHelper;

	private int selectionIndex;

	private HGButton leftButton;

	private HGButton rightButton;

	private HGButton carouselButton;

	[NotNull]
	public Choice[] choices
	{
		get
		{
			return _choices;
		}
		set
		{
			_choices = value;
			UpdateFromCurrentValue();
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		if (controlsSubsection)
		{
			SetSettingState(subsectionHelper.AreAnyChildSettingsEnabled());
		}
	}

	protected override void OnUpdateControls()
	{
		UpdateFromCurrentValue();
	}

	public void ToggleArrowsInteractable(bool isInteractable)
	{
		if (leftButton == null)
		{
			leftButton = leftArrowButton.GetComponent<HGButton>();
		}
		if (rightButton == null)
		{
			rightButton = rightArrowButton.GetComponent<HGButton>();
		}
		if (carouselButton == null)
		{
			carouselButton = base.gameObject.GetComponent<HGButton>();
		}
		if (leftButton != null && rightButton != null && carouselButton != null)
		{
			leftButton.interactable = isInteractable;
			rightButton.interactable = isInteractable;
			carouselButton.interactable = isInteractable;
		}
	}

	public void MoveCarousel(int direction)
	{
		selectionIndex = Mathf.Clamp(selectionIndex + direction, 0, choices.Length - 1);
		UpdateFromSelectionIndex();
		SubmitSetting(choices[selectionIndex].convarValue);
	}

	public void BoolCarousel()
	{
		selectionIndex = ((selectionIndex == 0) ? 1 : 0);
		UpdateFromSelectionIndex();
		if (controlsSubsection)
		{
			subsectionHelper.ApplyStateToChildSettings(selectionIndex == 1);
		}
		SubmitSetting(choices[selectionIndex].convarValue);
	}

	public override void SetSettingState(bool turnedOn)
	{
		selectionIndex = (turnedOn ? 1 : 0);
		UpdateFromSelectionIndex();
		SubmitSetting(choices[selectionIndex].convarValue);
	}

	public override bool IsSettingEnabled()
	{
		return selectionIndex > 0;
	}

	private void UpdateFromCurrentValue()
	{
		if (controlsSubsection)
		{
			return;
		}
		string currentValue = GetCurrentValue();
		bool flag = false;
		for (int i = 0; i < choices.Length; i++)
		{
			if (choices[i].convarValue == currentValue)
			{
				flag = true;
				selectionIndex = i;
				break;
			}
		}
		if (!flag && forceValidChoice)
		{
			selectionIndex = 0;
		}
		UpdateFromSelectionIndex();
	}

	private void UpdateFromSelectionIndex()
	{
		string text = "OPTION_CUSTOM";
		Sprite sprite = null;
		if (0 <= selectionIndex && selectionIndex < choices.Length)
		{
			Choice choice = choices[selectionIndex];
			text = choice.suboptionDisplayToken;
			sprite = choice.customSprite;
		}
		else if (choices.Length == 0)
		{
			text = string.Empty;
		}
		if ((bool)optionalText)
		{
			optionalText.GetComponent<LanguageTextMeshController>().token = text ?? string.Empty;
		}
		if ((bool)optionalImage)
		{
			optionalImage.sprite = sprite;
		}
	}

	protected override void Update()
	{
		base.Update();
		bool active = true;
		bool active2 = true;
		if (selectionIndex == 0)
		{
			active = false;
		}
		if (selectionIndex == choices.Length - 1)
		{
			active2 = false;
		}
		if ((bool)leftArrowButton)
		{
			leftArrowButton.SetActive(active);
		}
		if ((bool)rightArrowButton)
		{
			rightArrowButton.SetActive(active2);
		}
	}
}
