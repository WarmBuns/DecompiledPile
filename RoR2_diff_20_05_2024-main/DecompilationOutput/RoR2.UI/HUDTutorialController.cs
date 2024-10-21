using EntityStates;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(HUD))]
public class HUDTutorialController : MonoBehaviour
{
	private HUD hud;

	[Header("Equipment Tutorial")]
	[Tooltip("The tutorial popup object.")]
	public GameObject equipmentTutorialObject;

	[Tooltip("The equipment icon to monitor for a change to trigger the tutorial popup.")]
	public EquipmentIcon equipmentIcon;

	[Header("Sprint Tutorial")]
	[Tooltip("The tutorial popup object.")]
	public GameObject sprintTutorialObject;

	[Tooltip("How long to wait for the player to sprint before showing the tutorial popup.")]
	public float sprintTutorialTriggerTime = 30f;

	private float sprintTutorialStopwatch;

	private void Awake()
	{
		hud = GetComponent<HUD>();
		if ((bool)equipmentTutorialObject)
		{
			equipmentTutorialObject.SetActive(value: false);
		}
		if ((bool)sprintTutorialObject)
		{
			sprintTutorialObject.SetActive(value: false);
		}
	}

	private UserProfile GetUserProfile()
	{
		if ((bool)hud && hud.localUserViewer != null)
		{
			return hud.localUserViewer.userProfile;
		}
		return null;
	}

	private void HandleTutorial(GameObject tutorialPopup, ref UserProfile.TutorialProgression tutorialProgression, bool dismiss = false, bool progress = true)
	{
		if ((bool)tutorialPopup && !dismiss)
		{
			tutorialPopup.SetActive(value: true);
		}
		tutorialProgression.shouldShow = false;
		if (progress)
		{
			tutorialProgression.showCount++;
		}
	}

	private void Update()
	{
		if (!hud || hud.localUserViewer == null)
		{
			return;
		}
		UserProfile userProfile = GetUserProfile();
		CharacterBody cachedBody = hud.localUserViewer.cachedBody;
		if (userProfile == null || !cachedBody)
		{
			return;
		}
		if (userProfile.tutorialEquipment.shouldShow && equipmentIcon.hasEquipment)
		{
			HandleTutorial(equipmentTutorialObject, ref userProfile.tutorialEquipment);
		}
		if (userProfile.tutorialSprint.shouldShow)
		{
			if (cachedBody.isSprinting)
			{
				HandleTutorial(null, ref userProfile.tutorialSprint, dismiss: true);
				return;
			}
			if (cachedBody.GetComponent<EntityStateMachine>()?.state is GenericCharacterMain)
			{
				sprintTutorialStopwatch += Time.deltaTime;
			}
			if (sprintTutorialStopwatch >= sprintTutorialTriggerTime)
			{
				HandleTutorial(sprintTutorialObject, ref userProfile.tutorialSprint, dismiss: false, progress: false);
			}
		}
		else if ((bool)sprintTutorialObject && sprintTutorialObject.activeInHierarchy && cachedBody.isSprinting)
		{
			Object.Destroy(sprintTutorialObject);
			sprintTutorialObject = null;
			userProfile.tutorialSprint.showCount++;
		}
	}
}
