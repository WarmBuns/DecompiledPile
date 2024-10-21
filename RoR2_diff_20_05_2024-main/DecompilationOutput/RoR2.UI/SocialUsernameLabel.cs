using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SocialUsernameLabel : MonoBehaviour
{
	protected TextMeshProUGUI textMeshComponent;

	private PlatformID _userId;

	public int subPlayerIndex;

	public PlatformID userId
	{
		get
		{
			return _userId;
		}
		set
		{
			_userId = value;
		}
	}

	private void Awake()
	{
		textMeshComponent = GetComponent<TextMeshProUGUI>();
	}

	public virtual void Refresh()
	{
		if (textMeshComponent != null)
		{
			textMeshComponent.text = PlatformSystems.lobbyManager.GetUserDisplayName(_userId);
		}
	}
}
