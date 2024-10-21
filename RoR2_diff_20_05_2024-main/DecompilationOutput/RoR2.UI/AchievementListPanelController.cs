using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class AchievementListPanelController : MonoBehaviour
{
	public GameObject achievementCardPrefab;

	public RectTransform container;

	private MPEventSystemLocator eventSystemLocator;

	private readonly List<AchievementCardController> cardsList = new List<AchievementCardController>();

	private static readonly List<string> sortedAchievementIdentifiers;

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	private void OnEnable()
	{
		Rebuild();
	}

	private UserProfile GetUserProfile()
	{
		MPEventSystem eventSystem = eventSystemLocator.eventSystem;
		if ((bool)eventSystem)
		{
			LocalUser localUser = LocalUserManager.FindLocalUser(eventSystem.player);
			if (localUser != null)
			{
				return localUser.userProfile;
			}
		}
		return null;
	}

	static AchievementListPanelController()
	{
		sortedAchievementIdentifiers = new List<string>();
		BuildAchievementListOrder();
		AchievementManager.onAchievementsRegistered += BuildAchievementListOrder;
	}

	private static void BuildAchievementListOrder()
	{
		sortedAchievementIdentifiers.Clear();
		HashSet<string> encounteredIdentifiers = new HashSet<string>();
		ReadOnlyCollection<string> readOnlyAchievementIdentifiers = AchievementManager.readOnlyAchievementIdentifiers;
		for (int i = 0; i < readOnlyAchievementIdentifiers.Count; i++)
		{
			string achievementIdentifier = readOnlyAchievementIdentifiers[i];
			if (string.IsNullOrEmpty(AchievementManager.GetAchievementDef(achievementIdentifier).prerequisiteAchievementIdentifier))
			{
				AddAchievementToOrderedList(achievementIdentifier, encounteredIdentifiers);
			}
		}
	}

	private static void AddAchievementToOrderedList(string achievementIdentifier, HashSet<string> encounteredIdentifiers)
	{
		if (!encounteredIdentifiers.Contains(achievementIdentifier))
		{
			encounteredIdentifiers.Add(achievementIdentifier);
			sortedAchievementIdentifiers.Add(achievementIdentifier);
			string[] childAchievementIdentifiers = AchievementManager.GetAchievementDef(achievementIdentifier).childAchievementIdentifiers;
			for (int i = 0; i < childAchievementIdentifiers.Length; i++)
			{
				AddAchievementToOrderedList(childAchievementIdentifiers[i], encounteredIdentifiers);
			}
		}
	}

	private void SetCardCount(int desiredCardCount)
	{
		while (cardsList.Count < desiredCardCount)
		{
			AchievementCardController component = Object.Instantiate(achievementCardPrefab, container).GetComponent<AchievementCardController>();
			cardsList.Add(component);
		}
		while (cardsList.Count > desiredCardCount)
		{
			Object.Destroy(cardsList[cardsList.Count - 1].gameObject);
			cardsList.RemoveAt(cardsList.Count - 1);
		}
	}

	private void Rebuild()
	{
		UserProfile userProfile = GetUserProfile();
		SetCardCount(sortedAchievementIdentifiers.Count);
		for (int i = 0; i < sortedAchievementIdentifiers.Count; i++)
		{
			cardsList[i].SetAchievement(sortedAchievementIdentifiers[i], userProfile);
		}
	}
}
