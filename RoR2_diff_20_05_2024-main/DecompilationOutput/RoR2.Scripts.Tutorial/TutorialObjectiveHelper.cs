using RoR2.UI;
using UnityEngine;

namespace RoR2.Scripts.Tutorial;

public class TutorialObjectiveHelper : MonoBehaviour
{
	public GameObject SearchRoot;

	public HGTextMeshProUGUI TextToUpdate;

	private ObjectivePanelController _objectivePanelController;

	private TutorialObjectiveCrossfadeHelper _crossfadeHelper;

	private void Start()
	{
		if ((bool)SearchRoot)
		{
			ObjectivePanelController componentInChildren = SearchRoot.GetComponentInChildren<ObjectivePanelController>();
			if ((object)componentInChildren != null)
			{
				_objectivePanelController = componentInChildren;
			}
		}
	}

	public void GrabText()
	{
		if ((bool)_objectivePanelController && _objectivePanelController.GetPrimaryObjectiveTracker(out var primaryObjectiveTracker))
		{
			TextToUpdate.text = primaryObjectiveTracker.GetString();
			_crossfadeHelper = primaryObjectiveTracker.stripObject.GetComponent<TutorialObjectiveCrossfadeHelper>();
		}
	}

	public void TriggerObjectiveSideCrossfade()
	{
		if ((bool)_crossfadeHelper)
		{
			_crossfadeHelper.FinishCrossfade();
		}
	}
}
