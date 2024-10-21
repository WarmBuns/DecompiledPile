using UnityEngine;

namespace RoR2.UI;

public class HudObjectiveTargetSetter : MonoBehaviour
{
	public ObjectivePanelController objectivePanelController;

	private HUD hud;

	private void OnEnable()
	{
		hud = GetComponentInParent<HUD>();
	}

	private void Update()
	{
		if ((bool)hud && (bool)objectivePanelController)
		{
			objectivePanelController.SetCurrentMaster(hud.targetMaster);
		}
	}
}
