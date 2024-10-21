using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class SteamLobbyOwnerFilter : MonoBehaviour
{
	public Button[] buttonsToFilter;

	public GameObject[] objectsToFilter;

	private bool wasOn;

	private void Start()
	{
		Refresh(forceRefresh: true);
	}

	private void Update()
	{
		Refresh();
	}

	private void Refresh(bool forceRefresh = false)
	{
		bool ownsLobby = PlatformSystems.lobbyManager.ownsLobby;
		if (ownsLobby != wasOn || forceRefresh)
		{
			for (int i = 0; i < objectsToFilter.Length; i++)
			{
				objectsToFilter[i].SetActive(ownsLobby);
			}
			for (int j = 0; j < buttonsToFilter.Length; j++)
			{
				buttonsToFilter[j].interactable = ownsLobby;
			}
		}
		wasOn = ownsLobby;
	}
}
