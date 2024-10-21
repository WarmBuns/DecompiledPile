using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(LanguageTextMeshController))]
public class SteamLobbyStatusText : MonoBehaviour
{
	private LanguageTextMeshController languageTextMeshController;

	private void Start()
	{
		languageTextMeshController = GetComponent<LanguageTextMeshController>();
	}

	private void Update()
	{
		LobbyType currentLobbyType = PlatformSystems.lobbyManager.currentLobbyType;
		if (currentLobbyType == LobbyType.Error)
		{
			return;
		}
		for (int i = 0; i < LobbyUserList.lobbyStateChoices.Length; i++)
		{
			if (currentLobbyType == LobbyUserList.lobbyStateChoices[i].lobbyType)
			{
				languageTextMeshController.token = LobbyUserList.lobbyStateChoices[i].token;
				break;
			}
		}
	}
}
