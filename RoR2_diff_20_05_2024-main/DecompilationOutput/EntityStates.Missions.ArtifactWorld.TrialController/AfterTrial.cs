using System;
using RoR2;
using UnityEngine;

namespace EntityStates.Missions.ArtifactWorld.TrialController;

public class AfterTrial : ArtifactTrialControllerBaseState
{
	public virtual Type GetNextStateType()
	{
		return typeof(FinishTrial);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		purchaseInteraction.enabled = true;
		childLocator.FindChild("AfterTrial").gameObject.SetActive(value: true);
		outer.mainStateType = new SerializableEntityStateType(GetNextStateType());
		Highlight component = GetComponent<Highlight>();
		Transform transform = childLocator.FindChild("CompletedArtifactMesh");
		if ((bool)component && (bool)transform)
		{
			component.targetRenderer = transform.GetComponent<MeshRenderer>();
		}
	}

	public override void OnExit()
	{
		childLocator.FindChild("AfterTrial").gameObject.SetActive(value: false);
		base.OnExit();
	}
}
