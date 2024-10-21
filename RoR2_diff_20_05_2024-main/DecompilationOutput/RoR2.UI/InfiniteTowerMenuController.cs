using RoR2.Stats;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class InfiniteTowerMenuController : MonoBehaviour
{
	[SerializeField]
	private LanguageTextMeshController survivorNameText;

	[SerializeField]
	private LanguageTextMeshController easyHighestWaveText;

	[SerializeField]
	private LanguageTextMeshController normalHighestWaveText;

	[SerializeField]
	private LanguageTextMeshController hardHighestWaveText;

	private LocalUser localUser;

	private MPEventSystemLocator eventSystemLocator;

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	private void OnEnable()
	{
		UserProfile.onSurvivorPreferenceChangedGlobal += OnSurvivorPreferenceChangedGlobal;
		eventSystemLocator.onEventSystemDiscovered += OnEventSystemDiscovered;
		if ((bool)eventSystemLocator.eventSystem)
		{
			OnEventSystemDiscovered(eventSystemLocator.eventSystem);
		}
	}

	private void OnDisable()
	{
		eventSystemLocator.onEventSystemDiscovered -= OnEventSystemDiscovered;
		UserProfile.onSurvivorPreferenceChangedGlobal -= OnSurvivorPreferenceChangedGlobal;
		localUser = null;
	}

	private void OnSurvivorPreferenceChangedGlobal(UserProfile userProfile)
	{
		UpdateDisplayedSurvivor();
	}

	public void BeginGamemode()
	{
		Console.instance.SubmitCmd(null, "transition_command \"gamemode InfiniteTowerRun; host 0;\"");
	}

	public void SetDisplayedSurvivor(SurvivorDef newSurvivorDef)
	{
		_ = (bool)newSurvivorDef;
	}

	private void OnEventSystemDiscovered(MPEventSystem eventSystem)
	{
		localUser = eventSystem.localUser;
		UpdateDisplayedSurvivor();
	}

	private void UpdateDisplayedSurvivor()
	{
		UserProfile userProfile = eventSystemLocator.eventSystem?.localUser?.userProfile;
		if (userProfile != null)
		{
			StatSheet statSheet = userProfile.statSheet;
			SurvivorDef survivorPreference = userProfile.GetSurvivorPreference();
			if ((bool)survivorPreference)
			{
				string bodyName = BodyCatalog.GetBodyName(SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorPreference.survivorIndex));
				SetHighestWaveDisplay(easyHighestWaveText, PerBodyStatDef.highestInfiniteTowerWaveReachedEasy, statSheet, bodyName);
				SetHighestWaveDisplay(normalHighestWaveText, PerBodyStatDef.highestInfiniteTowerWaveReachedNormal, statSheet, bodyName);
				SetHighestWaveDisplay(hardHighestWaveText, PerBodyStatDef.highestInfiniteTowerWaveReachedHard, statSheet, bodyName);
			}
			if ((bool)survivorNameText)
			{
				survivorNameText.token = survivorPreference.displayNameToken;
			}
		}
	}

	private void SetHighestWaveDisplay(LanguageTextMeshController text, PerBodyStatDef statDef, StatSheet statSheet, string bodyName)
	{
		ulong statValueULong = statSheet.GetStatValueULong(statDef, bodyName);
		text.formatArgs = new object[1] { statValueULong };
	}
}
