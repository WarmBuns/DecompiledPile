using UnityEngine;

namespace EntityStates.BeetleQueenMonster;

public class BeginBurrow : BaseState
{
	public static GameObject burrowPrefab;

	public static float duration;

	private bool isBurrowing;

	private Animator animator;

	private Transform modelTransform;

	private ChildLocator childLocator;

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		PlayCrossfade("Body", "BeginBurrow", "BeginBurrow.playbackRate", duration, 0.5f);
		modelTransform = GetModelTransform();
		childLocator = modelTransform.GetComponent<ChildLocator>();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		bool flag = animator.GetFloat("BeginBurrow.active") > 0.9f;
		if (flag && !isBurrowing)
		{
			string childName = "BurrowCenter";
			Transform transform = childLocator.FindChild(childName);
			if ((bool)transform)
			{
				Object.Instantiate(burrowPrefab, transform.position, Quaternion.identity);
			}
		}
		isBurrowing = flag;
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
