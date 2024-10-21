using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SetLabelTextToMainUserProfileName : MonoBehaviour
{
	private TextMeshProUGUI label;

	private void Awake()
	{
		label = GetComponent<TextMeshProUGUI>();
	}

	private void OnEnable()
	{
		Apply();
	}

	private void Apply()
	{
		if (LocalUserManager.FindLocalUser(0) != null)
		{
			string userName = PlatformSystems.userManager.GetUserName();
			label.text = userName;
		}
		else
		{
			label.text = string.Format(Language.GetString("TITLE_PROFILE"), "");
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Language.onCurrentLanguageChanged += OnCurrentLanguageChanged;
		LocalUserManager.onUserSignIn += OnUserSignedIn;
		UserManager.OnUserStateChanged += OnCurrentLanguageChanged;
	}

	private static void OnUserSignedIn(LocalUser obj)
	{
		OnCurrentLanguageChanged();
	}

	private static void OnCurrentLanguageChanged()
	{
		SetLabelTextToMainUserProfileName[] array = Object.FindObjectsOfType<SetLabelTextToMainUserProfileName>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Apply();
		}
	}
}
