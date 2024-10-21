using RoR2;
using UnityEngine;

namespace EntityStates.InfiniteTowerSafeWard;

public class BaseSafeWardState : EntityState
{
	protected PurchaseInteraction purchaseInteraction;

	protected VerticalTubeZone zone;

	protected Animator animator;

	[SerializeField]
	public string objectiveToken;

	private GenericObjectiveProvider genericObjectiveProvider;

	protected InfiniteTowerSafeWardController safeWardController;

	public override void OnEnter()
	{
		base.OnEnter();
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		zone = GetComponent<VerticalTubeZone>();
		animator = base.gameObject.GetComponentInChildren<Animator>();
		safeWardController = base.gameObject.GetComponent<InfiniteTowerSafeWardController>();
		if (!string.IsNullOrEmpty(objectiveToken))
		{
			genericObjectiveProvider = base.gameObject.AddComponent<GenericObjectiveProvider>();
			genericObjectiveProvider.objectiveToken = objectiveToken;
		}
	}

	public override void PlayAnimation(string layerName, string animationStateName)
	{
		if ((bool)animator && !string.IsNullOrEmpty(layerName))
		{
			EntityState.PlayAnimationOnAnimator(animator, layerName, animationStateName);
		}
	}

	public override void OnExit()
	{
		if ((bool)genericObjectiveProvider)
		{
			EntityState.Destroy(genericObjectiveProvider);
		}
		base.OnExit();
	}
}
