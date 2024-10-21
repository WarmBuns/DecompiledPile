using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2.HudOverlay;
using RoR2.UI;
using UnityEngine;

namespace RoR2.VoidRaidCrab;

public class VoidRaidCrabHealthBarOverlayProvider : MonoBehaviour
{
	[SerializeField]
	private GameObject healthbarOverlayPrefab;

	[SerializeField]
	private string overlayChildLocatorEntryName;

	[SerializeField]
	private PhasedInventorySetter phasedInventorySetter;

	private List<OverlayController> overlayControllers = new List<OverlayController>();

	private void OnEnable()
	{
		overlayControllers.Clear();
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Player);
		overlayControllers = new List<OverlayController>();
		foreach (TeamComponent item in teamMembers)
		{
			OverlayController overlayController = HudOverlayManager.AddOverlay(item.gameObject, new OverlayCreationParams
			{
				prefab = healthbarOverlayPrefab,
				childLocatorEntry = overlayChildLocatorEntryName
			});
			overlayController.onInstanceAdded += OnOverlayInstanceAdded;
			overlayControllers.Add(overlayController);
		}
	}

	private void OnOverlayInstanceAdded(OverlayController overlayController, GameObject instance)
	{
		if ((bool)instance && (bool)phasedInventorySetter)
		{
			VoidRaidCrabHealthBarPipController component = instance.GetComponent<VoidRaidCrabHealthBarPipController>();
			if ((bool)component)
			{
				component.InitializePips(phasedInventorySetter);
			}
		}
	}

	private void OnDisable()
	{
		foreach (OverlayController overlayController in overlayControllers)
		{
			HudOverlayManager.RemoveOverlay(overlayController);
		}
		overlayControllers.Clear();
	}
}
