using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class ResolutionControl : BaseSettingsControl
{
	private class ResolutionOption
	{
		public Vector2Int size;

		public readonly List<int> supportedRefreshRates = new List<int>();

		public string GenerateDisplayString()
		{
			return $"{size.x}x{size.y}";
		}
	}

	public MPDropdown resolutionDropdown;

	public MPDropdown refreshRateDropdown;

	private Resolution[] resolutions;

	private ResolutionOption[] resolutionOptions = Array.Empty<ResolutionOption>();

	private static Vector2Int ResolutionToVector2Int(Resolution resolution)
	{
		return new Vector2Int(resolution.width, resolution.height);
	}

	private ResolutionOption GetCurrentSelectedResolutionOption()
	{
		if (resolutionDropdown.value >= 0)
		{
			return resolutionOptions[resolutionDropdown.value];
		}
		return null;
	}

	private void GenerateResolutionOptions()
	{
		Resolution[] array = Screen.resolutions;
		resolutionOptions = (from v in array.Select(ResolutionToVector2Int).Distinct()
			select new ResolutionOption
			{
				size = v
			}).ToArray();
		ResolutionOption[] array2 = resolutionOptions;
		foreach (ResolutionOption resolutionOption in array2)
		{
			Resolution[] array3 = array;
			for (int j = 0; j < array3.Length; j++)
			{
				Resolution resolution = array3[j];
				if (ResolutionToVector2Int(resolution) == resolutionOption.size)
				{
					resolutionOption.supportedRefreshRates.Add(resolution.refreshRate);
				}
			}
		}
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		array2 = resolutionOptions;
		foreach (ResolutionOption resolutionOption2 in array2)
		{
			list.Add(new TMP_Dropdown.OptionData
			{
				text = resolutionOption2.GenerateDisplayString()
			});
		}
		resolutionDropdown.ClearOptions();
		resolutionDropdown.AddOptions(list);
		int value = -1;
		Vector2Int vector2Int = ResolutionToVector2Int(Screen.currentResolution);
		for (int k = 0; k < resolutionOptions.Length; k++)
		{
			if (vector2Int == resolutionOptions[k].size)
			{
				value = k;
				break;
			}
		}
		resolutionDropdown.value = value;
	}

	private void GenerateRefreshRateOptions()
	{
		refreshRateDropdown.ClearOptions();
		ResolutionOption currentSelectedResolutionOption = GetCurrentSelectedResolutionOption();
		if (currentSelectedResolutionOption == null)
		{
			return;
		}
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		foreach (int supportedRefreshRate in currentSelectedResolutionOption.supportedRefreshRates)
		{
			list.Add(new TMP_Dropdown.OptionData(supportedRefreshRate + "Hz"));
		}
		refreshRateDropdown.AddOptions(list);
		int num = currentSelectedResolutionOption.supportedRefreshRates.IndexOf(Screen.currentResolution.refreshRate);
		if (num == -1)
		{
			num = currentSelectedResolutionOption.supportedRefreshRates.Count - 1;
		}
		refreshRateDropdown.value = num;
	}

	protected new void Awake()
	{
		base.Awake();
		resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownValueChanged);
		refreshRateDropdown.onValueChanged.AddListener(OnRefreshRateDropdownValueChanged);
	}

	protected new void OnEnable()
	{
		base.OnEnable();
		GenerateResolutionOptions();
		GenerateRefreshRateOptions();
	}

	private void OnResolutionDropdownValueChanged(int newValue)
	{
		if (newValue >= 0)
		{
			GenerateRefreshRateOptions();
		}
	}

	private void OnRefreshRateDropdownValueChanged(int newValue)
	{
		_ = 0;
	}

	public void SubmitCurrentValue()
	{
		if (resolutionDropdown.value != -1 && refreshRateDropdown.value != -1)
		{
			ResolutionOption resolutionOption = resolutionOptions[resolutionDropdown.value];
			SubmitSetting(string.Format(CultureInfo.InvariantCulture, "{0}x{1}x{2}", resolutionOption.size.x, resolutionOption.size.y, resolutionOption.supportedRefreshRates[refreshRateDropdown.value]));
		}
	}
}
