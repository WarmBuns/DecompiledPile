using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class EclipseRunScreenController : MonoBehaviour
{
	[Header("Required references")]
	public LanguageTextMeshController survivorName;

	public LanguageTextMeshController eclipseDifficultyName;

	public LanguageTextMeshController eclipseDifficultyDescription;

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
		Console.instance.SubmitCmd(null, "transition_command \"gamemode EclipseRun; host 0;\"");
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
		_ = string.Empty;
		string token = string.Empty;
		string token2 = string.Empty;
		SurvivorDef survivorDef = eventSystemLocator.eventSystem?.localUser?.userProfile.GetSurvivorPreference();
		if ((bool)survivorDef)
		{
			DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(EclipseRun.GetEclipseDifficultyIndex(Mathf.Clamp(EclipseRun.GetLocalUserSurvivorCompletedEclipseLevel(localUser, survivorDef) + 1, EclipseRun.minEclipseLevel, EclipseRun.maxEclipseLevel)));
			token = difficultyDef.nameToken;
			token2 = difficultyDef.descriptionToken;
		}
		if ((bool)survivorName)
		{
			survivorName.token = survivorDef.displayNameToken;
		}
		if ((bool)eclipseDifficultyName)
		{
			eclipseDifficultyName.token = token;
		}
		if ((bool)eclipseDifficultyDescription)
		{
			eclipseDifficultyDescription.token = token2;
		}
	}
}
