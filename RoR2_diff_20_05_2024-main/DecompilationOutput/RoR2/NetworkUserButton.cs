using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2;

public class NetworkUserButton : MonoBehaviour
{
	public NetworkUser targetUser;

	public bool targetState;

	public string targetUserName;

	private RawImage classImage;

	public void SetUser(NetworkUser user)
	{
		targetUser = user;
		classImage = GetComponentInChildren<RawImage>();
		TextMeshProUGUI componentInChildren = GetComponentInChildren<TextMeshProUGUI>();
		if ((bool)classImage)
		{
			GameObject bodyPrefab = BodyCatalog.GetBodyPrefab(user.bodyIndexPreference);
			if ((bool)bodyPrefab)
			{
				CharacterBody component = bodyPrefab.GetComponent<CharacterBody>();
				if ((bool)component && component.portraitIcon != null)
				{
					classImage.texture = bodyPrefab.GetComponent<CharacterBody>().portraitIcon;
				}
			}
		}
		if ((bool)componentInChildren)
		{
			string nameOverride = user.GetNetworkPlayerName().nameOverride;
			if (nameOverride != null)
			{
				string text2 = (componentInChildren.text = nameOverride);
				targetUserName = text2;
			}
			else
			{
				string text2 = (componentInChildren.text = "???");
				targetUserName = text2;
			}
		}
	}

	public void ToggleUserTargetState()
	{
		targetState = !targetState;
		UpdateColor();
	}

	public void SetUserTargetState(bool state)
	{
		targetState = state;
		UpdateColor();
	}

	private void UpdateColor()
	{
		if (targetState)
		{
			classImage.color = Color.white;
		}
		else
		{
			classImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
		}
	}
}
