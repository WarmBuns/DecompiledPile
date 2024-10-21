using System;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class BaseSettingsControl : MonoBehaviour
{
	public enum SettingSource
	{
		ConVar,
		UserProfilePref,
		None
	}

	public SettingSource settingSource;

	[FormerlySerializedAs("convarName")]
	public string settingName;

	public string nameToken;

	public LanguageTextMeshController nameLabel;

	[Tooltip("Whether or not this setting requires a confirmation dialog. This is mainly for video options.")]
	public bool useConfirmationDialog;

	[Tooltip("Whether or not this updates every frame. Should be disabled unless the setting is being modified from some other source.")]
	public bool updateControlsInUpdate;

	public UnityAction onValueChanged;

	private MPEventSystemLocator eventSystemLocator;

	private static UserProfile defaultUserProfile;

	private string originalValue;

	public bool hasBeenChanged => originalValue != null;

	protected bool inUpdateControls { get; private set; }

	protected void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		if ((bool)nameLabel && !string.IsNullOrEmpty(nameToken))
		{
			nameLabel.token = nameToken;
		}
		if (settingSource == SettingSource.ConVar && GetConVar() == null)
		{
			Debug.LogErrorFormat("Null convar {0} detected in options", settingName);
		}
	}

	protected void Start()
	{
		Initialize();
		UpdateControls();
	}

	protected void OnEnable()
	{
		UpdateControls();
	}

	protected virtual void Update()
	{
		if (updateControlsInUpdate)
		{
			UpdateControls();
		}
	}

	public virtual void Initialize()
	{
	}

	public void SubmitSetting(string newValue)
	{
		if (useConfirmationDialog)
		{
			SubmitSettingTemporary(newValue);
		}
		else
		{
			SubmitSettingInternal(newValue);
		}
	}

	private void SubmitSettingInternal(string newValue)
	{
		if (originalValue == null)
		{
			originalValue = GetCurrentValue();
		}
		if (originalValue == newValue)
		{
			originalValue = null;
		}
		switch (settingSource)
		{
		case SettingSource.ConVar:
			GetConVar()?.AttemptSetString(newValue);
			break;
		case SettingSource.UserProfilePref:
			GetCurrentUserProfile()?.SetSaveFieldString(settingName, newValue);
			break;
		}
		RoR2Application.onNextUpdate += UpdateControls;
		onValueChanged?.Invoke();
	}

	private void SubmitSettingTemporary(string newValue)
	{
		string oldValue = GetCurrentValue();
		if (newValue == oldValue)
		{
			return;
		}
		SubmitSettingInternal(newValue);
		SimpleDialogBox dialogBox = SimpleDialogBox.Create();
		Action revertFunction = delegate
		{
			if ((bool)dialogBox)
			{
				SubmitSettingInternal(oldValue);
			}
		};
		float num = 10f;
		float timeEnd = Time.unscaledTime + num;
		MPButton revertButton = dialogBox.AddActionButton(delegate
		{
			revertFunction();
		}, "OPTION_REVERT", true);
		dialogBox.AddActionButton(delegate
		{
		}, "OPTION_ACCEPT", true);
		Action updateText = null;
		updateText = delegate
		{
			if ((bool)dialogBox)
			{
				int num2 = Mathf.FloorToInt(timeEnd - Time.unscaledTime);
				if (num2 < 0)
				{
					num2 = 0;
				}
				dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
				{
					token = "OPTION_AUTOREVERT_DIALOG_DESCRIPTION",
					formatParams = new object[1] { num2 }
				};
				if (num2 > 0)
				{
					RoR2Application.unscaledTimeTimers.CreateTimer(1f, updateText);
				}
			}
		};
		updateText();
		dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "OPTION_AUTOREVERT_DIALOG_TITLE"
		};
		RoR2Application.unscaledTimeTimers.CreateTimer(num, delegate
		{
			if ((bool)revertButton)
			{
				revertButton.onClick.Invoke();
			}
		});
	}

	public string GetCurrentValue()
	{
		return settingSource switch
		{
			SettingSource.ConVar => GetConVar()?.GetString(), 
			SettingSource.UserProfilePref => GetCurrentUserProfile()?.GetSaveFieldString(settingName) ?? "", 
			_ => "", 
		};
	}

	protected BaseConVar GetConVar()
	{
		return Console.instance.FindConVar(settingName);
	}

	public UserProfile GetCurrentUserProfile()
	{
		return eventSystemLocator.eventSystem?.localUser?.userProfile;
	}

	public void Revert()
	{
		if (hasBeenChanged)
		{
			SubmitSetting(originalValue);
			originalValue = null;
		}
	}

	public void ResetToDefault()
	{
		string newValue = null;
		switch (settingSource)
		{
		case SettingSource.ConVar:
			newValue = GetConVar()?.defaultValue;
			break;
		case SettingSource.UserProfilePref:
			newValue = GetOrCreateDefaultProfile()?.GetSaveFieldString(settingName) ?? "";
			break;
		}
		SubmitSetting(newValue);
	}

	private UserProfile GetOrCreateDefaultProfile()
	{
		if (defaultUserProfile == null)
		{
			defaultUserProfile = PlatformSystems.saveSystem.CreateGuestProfile();
		}
		return defaultUserProfile;
	}

	public virtual void SetSettingState(bool turnedOn)
	{
	}

	public virtual bool IsSettingEnabled()
	{
		return true;
	}

	protected void UpdateControls()
	{
		if ((bool)this && !inUpdateControls)
		{
			inUpdateControls = true;
			OnUpdateControls();
			inUpdateControls = false;
		}
	}

	protected virtual void OnUpdateControls()
	{
	}
}
