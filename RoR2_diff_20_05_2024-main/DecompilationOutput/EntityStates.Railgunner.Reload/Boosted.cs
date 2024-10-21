using RoR2;
using RoR2.HudOverlay;
using UnityEngine;

namespace EntityStates.Railgunner.Reload;

public class Boosted : BaseState
{
	[SerializeField]
	public float bonusDamageCoefficient;

	[SerializeField]
	public string boostConsumeSoundString;

	[SerializeField]
	public GameObject overlayPrefab;

	[SerializeField]
	public string overlayChildLocatorEntry;

	private OverlayController overlayController;

	public float GetBonusDamage()
	{
		return bonusDamageCoefficient * damageStat;
	}

	public void ConsumeBoost(bool queueReload)
	{
		Util.PlaySound(boostConsumeSoundString, base.gameObject);
		outer.SetNextState(new Waiting(queueReload));
	}

	public override void OnEnter()
	{
		base.OnEnter();
		OverlayCreationParams overlayCreationParams = default(OverlayCreationParams);
		overlayCreationParams.prefab = overlayPrefab;
		overlayCreationParams.childLocatorEntry = overlayChildLocatorEntry;
		OverlayCreationParams overlayCreationParams2 = overlayCreationParams;
		overlayController = HudOverlayManager.AddOverlay(base.gameObject, overlayCreationParams2);
	}

	public override void OnExit()
	{
		HudOverlayManager.RemoveOverlay(overlayController);
		base.OnExit();
	}
}
