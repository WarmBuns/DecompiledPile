using UnityEngine;

namespace RoR2.UI;

public class DifficultyTutorialController : MonoBehaviour
{
	private HUD hud;

	[Tooltip("The tutorial popup object.")]
	public GameObject difficultyTutorialObject;

	[Tooltip("The time at which to trigger the tutorial popup.")]
	public float difficultyTutorialTriggerTime = 60f;

	private void Awake()
	{
		hud = GetComponentInParent<HUD>();
		if ((bool)difficultyTutorialObject)
		{
			difficultyTutorialObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if ((bool)hud)
		{
			UserProfile userProfile = hud.localUserViewer.userProfile;
			CharacterBody cachedBody = hud.localUserViewer.cachedBody;
			if (userProfile != null && (bool)cachedBody && (bool)difficultyTutorialObject && userProfile.tutorialDifficulty.shouldShow && (bool)Run.instance && Run.instance.fixedTime >= difficultyTutorialTriggerTime)
			{
				difficultyTutorialObject.SetActive(value: true);
				userProfile.tutorialDifficulty.shouldShow = false;
				userProfile.tutorialDifficulty.showCount++;
			}
		}
	}
}
