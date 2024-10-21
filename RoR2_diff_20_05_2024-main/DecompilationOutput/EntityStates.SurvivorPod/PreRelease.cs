using RoR2;
using UnityEngine;

namespace EntityStates.SurvivorPod;

public class PreRelease : SurvivorPodBaseState
{
	private static int IdleToReleaseStateHash = Animator.StringToHash("IdleToRelease");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", IdleToReleaseStateHash);
		Util.PlaySound("Play_UI_podBlastDoorOpen", base.gameObject);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			modelTransform.GetComponent<ChildLocator>().FindChild("InitialExhaustFX").gameObject.SetActive(value: true);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Base");
			if (layerIndex != -1 && modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("IdleToReleaseFinished"))
			{
				outer.SetNextState(new Release());
			}
		}
	}
}
