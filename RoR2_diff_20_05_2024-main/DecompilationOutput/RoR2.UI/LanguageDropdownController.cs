using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class LanguageDropdownController : MonoBehaviour
{
	public MPDropdown dropdown;

	public GameObject stripPrefab;

	private Language[] languages = Array.Empty<Language>();

	private bool isProvidingPlatformOption;

	public void Awake()
	{
		dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
	}

	private void OnEnable()
	{
		SetupDropdownOptions();
	}

	private void SetupDropdownOptions()
	{
		dropdown.ClearOptions();
		languages = Language.GetAllLanguages().ToArray();
		string @string = Language.LanguageConVar.instance.GetString();
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>(languages.Length + 1);
		int num = -1;
		int num2 = -1;
		Language language = Language.FindLanguageByName(Language.GetPlatformLanguageName() ?? "");
		isProvidingPlatformOption = language != null;
		if (isProvidingPlatformOption)
		{
			list.Add(new TMP_Dropdown.OptionData(Language.GetStringFormatted("LANGUAGE_PLATFORM", language.selfName), language.iconSprite));
			num2++;
			if (@string == "platform")
			{
				num = num2;
			}
		}
		for (int i = 0; i < languages.Length; i++)
		{
			num2++;
			Language language2 = languages[i];
			list.Add(new TMP_Dropdown.OptionData(language2.selfName, language2.iconSprite));
			if (Language.currentLanguage == language2)
			{
				num = num2;
			}
		}
		dropdown.AddOptions(list);
		if (num != -1)
		{
			dropdown.SetValueWithoutNotify(num);
		}
	}

	private void OnDropdownValueChanged(int value)
	{
		if (isProvidingPlatformOption)
		{
			if (value == 0)
			{
				Language.LanguageConVar.instance.SetString("platform");
				return;
			}
			value--;
		}
		Language safe = ArrayUtils.GetSafe(languages, value);
		if (safe != null)
		{
			Language.LanguageConVar.instance.SetString(safe.name);
		}
	}
}
