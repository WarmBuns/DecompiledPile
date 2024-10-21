using RoR2.HudOverlay;
using UnityEngine;

namespace EntityStates.Seeker;

public class Meditate : BaseWindUp
{
	[SerializeField]
	public new GameObject scopeOverlayPrefab;

	private OverlayController overlayController;

	public override void OnEnter()
	{
		base.OnEnter();
		overlayController = HudOverlayManager.AddOverlay(base.gameObject, new OverlayCreationParams
		{
			prefab = scopeOverlayPrefab,
			childLocatorEntry = "ScopeContainer"
		});
	}
}
