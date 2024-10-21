using System;
using RoR2;
using UnityEngine;

namespace EntityStates.AurelioniteHeart;

public class AurelioniteHeartEntityState : EntityState
{
	protected AurelioniteHeartController heartController;

	protected Animator animator;

	protected Xoroshiro128Plus rng;

	protected Vector3 inertPosition;

	protected float heartMovementTimer;

	public static event Action falseSonUnlockEvent;

	public override void OnEnter()
	{
		base.OnEnter();
		heartController = base.gameObject.GetComponent<AurelioniteHeartController>();
		animator = heartController.heartAnimator;
		rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		inertPosition = new Vector3(base.gameObject.transform.position.x, base.gameObject.transform.position.y - 1f, base.gameObject.transform.position.z);
	}

	public void FalseSonUnlocked()
	{
		AurelioniteHeartEntityState.falseSonUnlockEvent?.Invoke();
	}
}
