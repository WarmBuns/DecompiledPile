using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;

namespace RoR2.UI;

public class LocalUserSignInController : MonoBehaviour
{
	public GameObject localUserCardPrefab;

	private readonly List<LocalUserSignInCardController> cards = new List<LocalUserSignInCardController>();

	private void Start()
	{
		LocalUserSignInCardController[] componentsInChildren = GetComponentsInChildren<LocalUserSignInCardController>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			cards.Add(componentsInChildren[i]);
		}
	}

	public bool AreAllCardsReady()
	{
		return cards.Any((LocalUserSignInCardController v) => v.rewiredPlayer != null && v.requestedUserProfile == null);
	}

	private void DoSignIn()
	{
		LocalUserManager.LocalUserInitializationInfo[] array = new LocalUserManager.LocalUserInitializationInfo[cards.Count((LocalUserSignInCardController v) => v.rewiredPlayer != null)];
		int index = 0;
		for (int i = 0; i < cards.Count; i++)
		{
			if (cards[i].rewiredPlayer != null)
			{
				array[index++] = new LocalUserManager.LocalUserInitializationInfo
				{
					player = cards[index].rewiredPlayer,
					profile = cards[index].requestedUserProfile
				};
			}
		}
		LocalUserManager.SetLocalUsers(array);
	}

	private LocalUserSignInCardController FindCardAssociatedWithRewiredPlayer(Player rewiredPlayer)
	{
		for (int i = 0; i < cards.Count; i++)
		{
			if (cards[i].rewiredPlayer == rewiredPlayer)
			{
				return cards[i];
			}
		}
		return null;
	}

	private LocalUserSignInCardController FindCardWithoutRewiredPlayer()
	{
		for (int i = 0; i < cards.Count; i++)
		{
			if (cards[i].rewiredPlayer == null)
			{
				return cards[i];
			}
		}
		return null;
	}

	private void Update()
	{
		IList<Player> players = ReInput.players.Players;
		for (int i = 0; i < players.Count; i++)
		{
			Player player = players[i];
			if (player.name == "PlayerMain")
			{
				continue;
			}
			LocalUserSignInCardController localUserSignInCardController = FindCardAssociatedWithRewiredPlayer(player);
			if (localUserSignInCardController == null)
			{
				if (player.GetButtonDown(11))
				{
					LocalUserSignInCardController localUserSignInCardController2 = FindCardWithoutRewiredPlayer();
					if (localUserSignInCardController2 != null)
					{
						localUserSignInCardController2.rewiredPlayer = player;
					}
				}
			}
			else if (player.GetButtonDown(15) || !PlayerHasControllerConnected(player))
			{
				localUserSignInCardController.rewiredPlayer = null;
			}
		}
		_ = LocalUserManager.readOnlyLocalUsersList;
		int num = 4;
		while (cards.Count < num)
		{
			cards.Add(Object.Instantiate(localUserCardPrefab, base.transform).GetComponent<LocalUserSignInCardController>());
		}
		while (cards.Count > num)
		{
			Object.Destroy(cards[cards.Count - 1].gameObject);
			cards.RemoveAt(cards.Count - 1);
		}
	}

	private static bool PlayerHasControllerConnected(Player player)
	{
		using (IEnumerator<Controller> enumerator = player.controllers.Controllers.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				_ = enumerator.Current;
				return true;
			}
		}
		return false;
	}
}
