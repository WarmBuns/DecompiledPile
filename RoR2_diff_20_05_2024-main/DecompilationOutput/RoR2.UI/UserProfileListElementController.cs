using System;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPButton))]
public class UserProfileListElementController : MonoBehaviour
{
	public TextMeshProUGUI nameLabel;

	private MPButton button;

	public TextMeshProUGUI playTimeLabel;

	[NonSerialized]
	public UserProfileListController listController;

	private UserProfile _userProfile;

	public UserProfile userProfile
	{
		get
		{
			return _userProfile;
		}
		set
		{
			if (_userProfile != value)
			{
				_userProfile = value;
				string sourceText = "???";
				uint num = 0u;
				if (_userProfile != null)
				{
					sourceText = _userProfile.name;
					num = _userProfile.totalLoginSeconds;
				}
				if ((bool)nameLabel)
				{
					nameLabel.SetText(sourceText);
				}
				if ((bool)playTimeLabel)
				{
					TimeSpan timeSpan = TimeSpan.FromSeconds(num);
					playTimeLabel.SetText($"{(uint)timeSpan.TotalHours}:{(uint)timeSpan.Minutes:D2}");
				}
			}
		}
	}

	private void Awake()
	{
		button = GetComponent<MPButton>();
		button.onClick.AddListener(InformListControllerOfSelection);
	}

	private void InformListControllerOfSelection()
	{
		if (!userProfile.isCorrupted)
		{
			listController.SendProfileSelection(userProfile);
		}
	}
}
