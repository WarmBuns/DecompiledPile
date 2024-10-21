using RoR2;
using UnityEngine;

namespace EntityStates.ShrineRebirth;

public class RebirthOrPortalChoice : ShrineRebirthEntityStates
{
	public override void OnEnter()
	{
		base.OnEnter();
		_shrineController.onRebirthOrContinueTextObject.SetActive(value: true);
		_shrineController.CallRpcChangeInteractScript();
		GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(_shrineController.helminthPortalISC, new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Direct,
			position = _shrineController.portalObject.transform.position,
			spawnOnTarget = _shrineController.portalObject.transform
		}, Run.instance.stageRng));
		if ((bool)gameObject)
		{
			gameObject.transform.rotation = _shrineController.portalObject.transform.rotation;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		_shrineController.onRebirthOrContinueTextObject.SetActive(value: true);
	}
}
