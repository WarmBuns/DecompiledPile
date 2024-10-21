using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class ProfileNameLabel : MonoBehaviour
{
	public string token;

	private MPEventSystemLocator eventSystemLocator;

	private TextMeshProUGUI label;

	private string currentUserName;

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		label = GetComponent<TextMeshProUGUI>();
	}

	private void LateUpdate()
	{
		string text = PlatformSystems.userManager.GetUserName() ?? string.Empty;
		if (text != currentUserName)
		{
			currentUserName = text;
			label.text = Language.GetStringFormatted(token, currentUserName);
		}
	}
}
