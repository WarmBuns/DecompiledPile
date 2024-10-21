using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2.SurvivorMannequins;

public class SurvivorMannequinDioramaController : MonoBehaviour
{
	public bool showLocalPlayersFirst = true;

	[SerializeField]
	private SurvivorMannequinSlotController[] mannequinSlots = Array.Empty<SurvivorMannequinSlotController>();

	private bool sortedNetworkUsersDirty = true;

	private List<NetworkUser> sortedNetworkUsers = new List<NetworkUser>();

	private void OnEnable()
	{
		NetworkUser.onNetworkUserDiscovered += OnNetworkUserDiscovered;
		NetworkUser.onNetworkUserLost += OnNetworkUserLost;
	}

	private void OnDisable()
	{
		NetworkUser.onNetworkUserLost -= OnNetworkUserLost;
		NetworkUser.onNetworkUserDiscovered -= OnNetworkUserDiscovered;
		sortedNetworkUsers.Clear();
		UpdateMannequins();
	}

	private void Update()
	{
		if (sortedNetworkUsersDirty)
		{
			sortedNetworkUsersDirty = false;
			UpdateSortedNetworkUsersList();
		}
		UpdateMannequins();
	}

	private void UpdateSortedNetworkUsersList()
	{
		sortedNetworkUsers.Clear();
		if (showLocalPlayersFirst)
		{
			ListUtils.AddRange(sortedNetworkUsers, NetworkUser.readOnlyLocalPlayersList);
			for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
			{
				ListUtils.AddIfUnique(sortedNetworkUsers, NetworkUser.readOnlyInstancesList[i]);
			}
		}
	}

	private void OnNetworkUserLost(NetworkUser networkUser)
	{
		sortedNetworkUsersDirty = true;
	}

	private void OnNetworkUserDiscovered(NetworkUser networkUser)
	{
		sortedNetworkUsersDirty = true;
	}

	public void GetSlots(List<SurvivorMannequinSlotController> dest)
	{
		if (dest == null)
		{
			throw new ArgumentNullException("dest");
		}
		ListUtils.AddRange(dest, mannequinSlots);
	}

	public void SetSlots(SurvivorMannequinSlotController[] newMannequinSlots)
	{
		if (newMannequinSlots == null)
		{
			throw new ArgumentNullException("newMannequinSlots");
		}
		for (int i = 0; i < mannequinSlots.Length; i++)
		{
			SurvivorMannequinSlotController survivorMannequinSlotController = mannequinSlots[i];
			if ((bool)survivorMannequinSlotController && Array.IndexOf(newMannequinSlots, survivorMannequinSlotController) == -1)
			{
				survivorMannequinSlotController.networkUser = null;
			}
		}
		ArrayUtils.CloneTo(newMannequinSlots, ref mannequinSlots);
		UpdateMannequins();
	}

	private void UpdateMannequins()
	{
		AssignNetworkUsersToSlots(sortedNetworkUsers);
	}

	private void AssignNetworkUsersToSlots(List<NetworkUser> networkUsers)
	{
		int i = 0;
		for (int num = Math.Min(networkUsers.Count, mannequinSlots.Length); i < num; i++)
		{
			NetworkUser networkUser = networkUsers[i];
			SurvivorMannequinSlotController survivorMannequinSlotController = mannequinSlots[i];
			if (!survivorMannequinSlotController || (object)survivorMannequinSlotController.networkUser == networkUser)
			{
				continue;
			}
			for (int j = i + 1; j < mannequinSlots.Length; j++)
			{
				SurvivorMannequinSlotController survivorMannequinSlotController2 = mannequinSlots[j];
				if ((bool)survivorMannequinSlotController2)
				{
					SurvivorMannequinSlotController.Swap(survivorMannequinSlotController, survivorMannequinSlotController2);
					break;
				}
			}
			survivorMannequinSlotController.networkUser = networkUser;
		}
		for (int k = networkUsers.Count; k < mannequinSlots.Length; k++)
		{
			mannequinSlots[k].networkUser = null;
		}
	}
}
