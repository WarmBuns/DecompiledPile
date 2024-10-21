using System.Globalization;
using UnityEngine;

namespace RoR2.UI;

internal class LobbyUserListElement
{
	public PlatformID id;

	public GameObject gameObject;

	public SocialUserIcon userIcon;

	public SteamUsernameLabel usernameLabel;

	public VoiceIcon voiceIcon;

	public ChildLocator elementChildLocator;

	public void SetUser(PlatformID playerUserID, int subPlayerIndex)
	{
		id = playerUserID;
		userIcon.RefreshWithUser(playerUserID);
		usernameLabel.userId = playerUserID;
		usernameLabel.subPlayerIndex = subPlayerIndex;
		Refresh();
	}

	public void Refresh(bool shouldForceRefresh = false)
	{
		if (id == default(PlatformID))
		{
			elementChildLocator.FindChild("UserIcon").gameObject.SetActive(value: false);
			elementChildLocator.FindChild("InviteButton").gameObject.SetActive(PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.Invite));
			elementChildLocator.FindChild("VoiceButton").gameObject.SetActive(value: false);
			elementChildLocator.FindChild("LobbyLeaderCrown").gameObject.SetActive(value: false);
		}
		else
		{
			elementChildLocator.FindChild("UserIcon").gameObject.SetActive(value: true);
			elementChildLocator.FindChild("InviteButton").gameObject.SetActive(value: false);
			elementChildLocator.FindChild("LobbyLeaderCrown").gameObject.SetActive(value: false);
		}
		userIcon.Refresh(shouldForceRefresh);
		usernameLabel.Refresh();
		voiceIcon?.Refresh();
		RefreshCrownAndPromoteButton();
	}

	private void RefreshCrownAndPromoteButton()
	{
		bool flag = PlatformSystems.lobbyManager.IsLobbyOwner(id) || (PlatformSystems.lobbyManager.GetLobbyMemberCount() == 1 && id != default(PlatformID));
		elementChildLocator.FindChild("LobbyLeaderCrown").gameObject.SetActive(flag);
		if (!elementChildLocator)
		{
			return;
		}
		bool flag2 = PlatformSystems.lobbyManager.ShouldShowPromoteButton() && !flag && PlatformSystems.lobbyManager.ownsLobby && id != default(PlatformID) && !NetworkSession.instance;
		GameObject gameObject = elementChildLocator.FindChild("PromoteButton").gameObject;
		if (!gameObject)
		{
			return;
		}
		gameObject.SetActive(flag2);
		if (!flag2)
		{
			return;
		}
		MPButton component = gameObject.GetComponent<MPButton>();
		if ((bool)component)
		{
			component.onClick.RemoveAllListeners();
			component.onClick.AddListener(delegate
			{
				Console.instance.SubmitCmd(null, string.Format(CultureInfo.InvariantCulture, "steam_lobby_assign_owner {0}", id));
			});
		}
	}
}
