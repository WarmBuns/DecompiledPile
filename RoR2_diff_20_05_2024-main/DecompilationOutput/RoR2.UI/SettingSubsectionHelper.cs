using System;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI;

public class SettingSubsectionHelper : MonoBehaviour
{
	[Tooltip("The setting entry responsible for representing the subsection state. Has to be an ON/OFF bool setting")]
	public CarouselController subsectionSettingEntry;

	private void Awake()
	{
		BaseSettingsControl[] componentsInChildren = GetComponentsInChildren<BaseSettingsControl>();
		foreach (BaseSettingsControl obj in componentsInChildren)
		{
			obj.onValueChanged = (UnityAction)Delegate.Combine(obj.onValueChanged, (UnityAction)delegate
			{
				if ((bool)subsectionSettingEntry)
				{
					subsectionSettingEntry.SetSettingState(AreAnyChildSettingsEnabled());
				}
			});
		}
	}

	public void ApplyStateToChildSettings(bool settingEnabled)
	{
		BaseSettingsControl[] componentsInChildren = GetComponentsInChildren<BaseSettingsControl>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetSettingState(settingEnabled);
		}
	}

	public bool AreAnyChildSettingsEnabled()
	{
		BaseSettingsControl[] componentsInChildren = GetComponentsInChildren<BaseSettingsControl>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].IsSettingEnabled())
			{
				return true;
			}
		}
		return false;
	}
}
