using RoR2;
using UnityEngine;
using UnityEngine.UI;

public class EclipseDifficultyMedalDisplay : MonoBehaviour
{
	[SerializeField]
	private int eclipseLevel;

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private Sprite unearnedSprite;

	[SerializeField]
	private Sprite incompleteSprite;

	[SerializeField]
	private Sprite completeSprite;

	private void OnEnable()
	{
		UserProfile.onSurvivorPreferenceChangedGlobal += OnSurvivorPreferenceChangedGlobal;
		Refresh();
	}

	private void OnDisable()
	{
		UserProfile.onSurvivorPreferenceChangedGlobal -= OnSurvivorPreferenceChangedGlobal;
	}

	private void OnSurvivorPreferenceChangedGlobal(UserProfile userProfile)
	{
		Refresh();
	}

	private void Refresh()
	{
		LocalUser firstLocalUser = LocalUserManager.GetFirstLocalUser();
		SurvivorDef survivorDef = firstLocalUser?.userProfile.GetSurvivorPreference();
		if (!survivorDef)
		{
			return;
		}
		int localUserSurvivorCompletedEclipseLevel = EclipseRun.GetLocalUserSurvivorCompletedEclipseLevel(firstLocalUser, survivorDef);
		if (eclipseLevel <= localUserSurvivorCompletedEclipseLevel)
		{
			bool flag = true;
			foreach (SurvivorDef orderedSurvivorDef in SurvivorCatalog.orderedSurvivorDefs)
			{
				if (ShouldDisplaySurvivor(orderedSurvivorDef, firstLocalUser))
				{
					localUserSurvivorCompletedEclipseLevel = EclipseRun.GetLocalUserSurvivorCompletedEclipseLevel(firstLocalUser, orderedSurvivorDef);
					if (localUserSurvivorCompletedEclipseLevel < eclipseLevel)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				iconImage.sprite = completeSprite;
			}
			else
			{
				iconImage.sprite = incompleteSprite;
			}
		}
		else
		{
			iconImage.sprite = unearnedSprite;
		}
	}

	private bool ShouldDisplaySurvivor(SurvivorDef survivorDef, LocalUser localUser)
	{
		if ((bool)survivorDef && !survivorDef.hidden)
		{
			return survivorDef.CheckUserHasRequiredEntitlement(localUser);
		}
		return false;
	}
}
