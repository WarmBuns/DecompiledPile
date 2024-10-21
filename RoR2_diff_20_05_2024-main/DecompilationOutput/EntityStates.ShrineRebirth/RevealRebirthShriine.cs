using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ShrineRebirth;

public class RevealRebirthShriine : ShrineRebirthEntityStates
{
	[SyncVar]
	private GameObject shrineModel;

	public override void OnEnter()
	{
		base.OnEnter();
		if (Run.instance.selectedDifficulty < DifficultyIndex.Eclipse1)
		{
			_shrineController.purchaseInteraction.SetAvailable(newAvailable: true);
			_shrineController.onShrineRevealObjectiveTextObject.SetActive(value: true);
			Transform transform = _shrineController.childLocator.FindChild("ShrineModel");
			if ((bool)transform)
			{
				shrineModel = transform.gameObject;
				if (!shrineModel.activeInHierarchy)
				{
					shrineModel.SetActive(value: true);
				}
			}
			return;
		}
		Transform transform2 = _shrineController.childLocator.FindChild("ShrineModel");
		shrineModel = transform2.gameObject;
		shrineModel.SetActive(value: false);
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
		_shrineController.onShrineRevealObjectiveTextObject.SetActive(value: false);
		base.OnExit();
	}
}
