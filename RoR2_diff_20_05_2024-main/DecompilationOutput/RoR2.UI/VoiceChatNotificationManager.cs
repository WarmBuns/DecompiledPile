using UnityEngine;

namespace RoR2.UI;

public class VoiceChatNotificationManager : MonoBehaviour
{
	public GameObject[] chatNameplate = new GameObject[3];

	public VoiceChatCardController[] chatController = new VoiceChatCardController[3];

	private PlatformID[] chatUserList;

	private PlatformID currentChatUser;

	private int frameDelay;

	private bool waitForInitialization;

	private void Awake()
	{
		if (!PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.Voice))
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (chatNameplate[0] == null)
		{
			HackFixRefrences();
		}
		chatUserList = PlatformSystems.lobbyManager.GetLobbyMembers();
		if (chatUserList == null)
		{
			waitForInitialization = true;
			for (int i = 0; i < 3; i++)
			{
				if (!(chatNameplate[i] == null))
				{
					chatNameplate[i].SetActive(value: false);
				}
			}
			return;
		}
		waitForInitialization = false;
		for (int j = 0; j < chatUserList.Length; j++)
		{
			if (j < 4)
			{
				_ = ref chatUserList[j];
				_ = chatUserList[j] == default(PlatformID);
			}
		}
		for (int k = 0; k < 3; k++)
		{
			if (!(chatNameplate[k] == null))
			{
				chatNameplate[k].SetActive(value: false);
			}
		}
	}

	private void Initialize()
	{
	}

	private void HackFixRefrences()
	{
	}

	private void Update()
	{
	}
}
