using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class LobbyUserList : MonoBehaviour
{
	public struct LobbyStateChoice
	{
		public LobbyType lobbyType;

		public string token;
	}

	public TextMeshProUGUI lobbyStateText;

	public GameObject lobbyControlPanel;

	public GameObject contentArea;

	public RectTransform playerListContainer;

	[Tooltip("The panel which acts as a container for UI that's only valid if there's an active lobby, like the lobby type or the copy-ID-to-clipboard button.")]
	public MPButton createLobbyButton;

	public MPButton leaveLobbyButton;

	public MPButton copyLobbyButton;

	public MPButton joinLobbyButton;

	public MPDropdown lobbyTypeDropdown;

	public bool shouldShowContentAreaByDefault = true;

	public bool isPauseMenu;

	private List<LobbyUserListElement> userList = new List<LobbyUserListElement>();

	public static readonly LobbyStateChoice[] lobbyStateChoices = new LobbyStateChoice[3]
	{
		new LobbyStateChoice
		{
			lobbyType = LobbyType.Private,
			token = "STEAM_LOBBY_PRIVATE"
		},
		new LobbyStateChoice
		{
			lobbyType = LobbyType.FriendsOnly,
			token = "STEAM_LOBBY_FRIENDSONLY"
		},
		new LobbyStateChoice
		{
			lobbyType = LobbyType.Public,
			token = "STEAM_LOBBY_PUBLIC"
		}
	};

	private static readonly List<string> optionsBuffer = new List<string>();

	public int NumberOfParticipants { get; private set; }

	private bool isInLobby => PlatformSystems.lobbyManager.isInLobby;

	private void Awake()
	{
		if (isPauseMenu && !PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.Voice))
		{
			base.gameObject.SetActive(value: false);
		}
		lobbyTypeDropdown.onValueChanged.AddListener(OnLobbyStateDropdownValueChanged);
	}

	private void OnEnable()
	{
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyChanged = (Action)Delegate.Combine(lobbyManager.onLobbyChanged, new Action(OnLobbyChanged));
		LobbyManager lobbyManager2 = PlatformSystems.lobbyManager;
		lobbyManager2.onLobbyDataUpdated = (Action)Delegate.Combine(lobbyManager2.onLobbyDataUpdated, new Action(RebuildPlayers));
		LobbyManager lobbyManager3 = PlatformSystems.lobbyManager;
		lobbyManager3.onLobbyStateChanged = (Action)Delegate.Combine(lobbyManager3.onLobbyStateChanged, new Action(OnLobbyStateChanged));
		LobbyManager lobbyManager4 = PlatformSystems.lobbyManager;
		lobbyManager4.onLobbyMemberDataUpdated = (Action<PlatformID>)Delegate.Combine(lobbyManager4.onLobbyMemberDataUpdated, new Action<PlatformID>(OnLobbyMemberDataUpdated));
		LobbyManager lobbyManager5 = PlatformSystems.lobbyManager;
		lobbyManager5.onPlayerCountUpdated = (Action)Delegate.Combine(lobbyManager5.onPlayerCountUpdated, new Action(RebuildPlayers));
		ToggleMPLobbyFeatures(PlatformSystems.lobbyManager.GetPlatformMPLobbyFeatureFlags());
		if (isPauseMenu)
		{
			if (!isInLobby)
			{
				base.gameObject.SetActive(value: false);
			}
			else
			{
				createLobbyButton.gameObject.SetActive(value: false);
				leaveLobbyButton.gameObject.SetActive(value: false);
				copyLobbyButton.gameObject.SetActive(value: false);
				joinLobbyButton.gameObject.SetActive(value: false);
				lobbyTypeDropdown.gameObject.SetActive(value: false);
				lobbyTypeDropdown.interactable = false;
			}
		}
		RebuildLobbyStateDropdownOptions();
		Refresh();
	}

	private void OnDisable()
	{
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyChanged = (Action)Delegate.Remove(lobbyManager.onLobbyChanged, new Action(OnLobbyChanged));
		LobbyManager lobbyManager2 = PlatformSystems.lobbyManager;
		lobbyManager2.onLobbyDataUpdated = (Action)Delegate.Remove(lobbyManager2.onLobbyDataUpdated, new Action(RebuildPlayers));
		LobbyManager lobbyManager3 = PlatformSystems.lobbyManager;
		lobbyManager3.onLobbyStateChanged = (Action)Delegate.Remove(lobbyManager3.onLobbyStateChanged, new Action(OnLobbyStateChanged));
		LobbyManager lobbyManager4 = PlatformSystems.lobbyManager;
		lobbyManager4.onLobbyMemberDataUpdated = (Action<PlatformID>)Delegate.Remove(lobbyManager4.onLobbyMemberDataUpdated, new Action<PlatformID>(OnLobbyMemberDataUpdated));
		LobbyManager lobbyManager5 = PlatformSystems.lobbyManager;
		lobbyManager5.onPlayerCountUpdated = (Action)Delegate.Remove(lobbyManager5.onPlayerCountUpdated, new Action(RebuildPlayers));
	}

	private void Update()
	{
		Refresh();
	}

	private void ToggleMPLobbyFeatures(MPLobbyFeatures lobbyFlags)
	{
		Toggle(createLobbyButton, lobbyFlags.HasFlag(MPLobbyFeatures.CreateLobby));
		Toggle(leaveLobbyButton, lobbyFlags.HasFlag(MPLobbyFeatures.LeaveLobby));
		Toggle(copyLobbyButton, lobbyFlags.HasFlag(MPLobbyFeatures.Clipboard));
		Toggle(joinLobbyButton, lobbyFlags.HasFlag(MPLobbyFeatures.Clipboard));
	}

	private void Toggle(MonoBehaviour component, bool val)
	{
		if ((bool)component)
		{
			component.gameObject.SetActive(val);
		}
	}

	public void DisplayContent(bool display)
	{
		if ((bool)contentArea)
		{
			contentArea.SetActive(display);
		}
	}

	public void Refresh()
	{
		if (PlatformSystems.lobbyManager.HasMPLobbyUI() || PlatformSystems.lobbyManager.HasMPFeatures())
		{
			if (lobbyControlPanel.activeSelf != isInLobby)
			{
				lobbyControlPanel.SetActive(isInLobby);
			}
			if (!isPauseMenu)
			{
				if ((bool)createLobbyButton && PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.CreateLobby) && createLobbyButton.gameObject.activeSelf == isInLobby)
				{
					createLobbyButton.gameObject.SetActive(!isInLobby);
				}
				if ((bool)leaveLobbyButton && PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.LeaveLobby) && leaveLobbyButton.gameObject.activeSelf != isInLobby)
				{
					leaveLobbyButton.gameObject.SetActive(isInLobby);
				}
				if ((bool)copyLobbyButton && PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.Clipboard))
				{
					if (PlatformSystems.ShouldUseEpicOnlineSystems && PlatformSystems.lobbyManager.currentLobbyType == LobbyType.Private)
					{
						copyLobbyButton.interactable = false;
						copyLobbyButton.gameObject.SetActive(value: true);
					}
					else
					{
						copyLobbyButton.interactable = isInLobby;
						copyLobbyButton.gameObject.SetActive(isInLobby);
					}
				}
				if (lobbyControlPanel.activeInHierarchy && PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.LobbyDropdownOptions))
				{
					lobbyTypeDropdown.interactable = PlatformSystems.lobbyManager.ownsLobby;
					LobbyType currentLobbyType = PlatformSystems.lobbyManager.currentLobbyType;
					for (int i = 0; i < lobbyStateChoices.Length; i++)
					{
						if (currentLobbyType == lobbyStateChoices[i].lobbyType)
						{
							lobbyTypeDropdown.SetValueWithoutNotify(i);
							break;
						}
					}
				}
			}
		}
		RebuildPlayers();
	}

	private void RebuildLobbyStateDropdownOptions()
	{
		if (PlatformSystems.lobbyManager.HasMPLobbyUI() && !isPauseMenu)
		{
			for (int i = 0; i < lobbyStateChoices.Length; i++)
			{
				optionsBuffer.Add(Language.GetString(lobbyStateChoices[i].token));
			}
			lobbyTypeDropdown.ClearOptions();
			lobbyTypeDropdown.AddOptions(optionsBuffer);
			optionsBuffer.Clear();
		}
	}

	private void OnLobbyStateDropdownValueChanged(int newValue)
	{
		if (isInLobby && !isPauseMenu)
		{
			PlatformSystems.lobbyManager.currentLobbyType = lobbyStateChoices[newValue].lobbyType;
			Refresh();
		}
	}

	public void ClearUserList()
	{
		while (userList.Count > 0)
		{
			int index = userList.Count - 1;
			UnityEngine.Object.Destroy(userList[index].gameObject);
			userList.RemoveAt(index);
		}
	}

	public void RebuildPlayers()
	{
		bool num = isInLobby;
		PlatformID[] lobbyMembers = PlatformSystems.lobbyManager.GetLobbyMembers();
		int num2 = ((!isPauseMenu) ? 1 : 0);
		int num3 = Math.Min(num ? (lobbyMembers.Length + num2) : 0, RoR2Application.maxPlayers);
		while (userList.Count > num3)
		{
			int index = userList.Count - 1;
			UnityEngine.Object.Destroy(userList[index].gameObject);
			userList.RemoveAt(index);
		}
		while (userList.Count < num3)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/LobbyUserListElement"), playerListContainer);
			gameObject.SetActive(value: true);
			SocialUserIcon componentInChildren = gameObject.GetComponentInChildren<SocialUserIcon>();
			VoiceIcon componentInChildren2 = gameObject.GetComponentInChildren<VoiceIcon>();
			SteamUsernameLabel componentInChildren3 = gameObject.GetComponentInChildren<SteamUsernameLabel>();
			ChildLocator component = gameObject.GetComponent<ChildLocator>();
			userList.Add(new LobbyUserListElement
			{
				gameObject = gameObject,
				userIcon = componentInChildren,
				voiceIcon = componentInChildren2,
				usernameLabel = componentInChildren3,
				elementChildLocator = component
			});
		}
		if (lobbyMembers != null)
		{
			int i;
			for (i = 0; i < lobbyMembers.Length && i < userList.Count; i++)
			{
				userList[i].SetUser(lobbyMembers[i], i);
			}
			for (; i < num3; i++)
			{
				userList[i].SetUser(default(PlatformID), 0);
			}
		}
	}

	private void UpdateUser(PlatformID userId)
	{
		for (int i = 0; i < userList.Count; i++)
		{
			if (userList[i].id == userId)
			{
				userList[i].Refresh(shouldForceRefresh: true);
			}
		}
	}

	private void OnLobbyStateChanged()
	{
		lobbyTypeDropdown.interactable = PlatformSystems.lobbyManager.ownsLobby;
		Refresh();
	}

	private void OnLobbyMemberDataUpdated(PlatformID steamId)
	{
		UpdateUser(steamId);
	}

	private void OnLobbyChanged()
	{
		Refresh();
	}
}
