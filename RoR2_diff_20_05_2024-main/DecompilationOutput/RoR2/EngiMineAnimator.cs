using EntityStates.Engi.Mine;
using RoR2.Projectile;
using UnityEngine;

namespace RoR2;

public class EngiMineAnimator : MonoBehaviour
{
	private Transform projectileTransform;

	public Animator animator;

	private EntityStateMachine armingStateMachine;

	private void Start()
	{
		ProjectileGhostController component = GetComponent<ProjectileGhostController>();
		if ((bool)component)
		{
			projectileTransform = component.authorityTransform;
			if ((bool)projectileTransform)
			{
				armingStateMachine = EntityStateMachine.FindByCustomName(projectileTransform.gameObject, "Arming");
			}
		}
	}

	private bool IsArmed()
	{
		return ((armingStateMachine?.state as BaseMineArmingState)?.damageScale ?? 0f) > 1f;
	}

	private void Update()
	{
		if (IsArmed())
		{
			animator.SetTrigger("Arming");
		}
	}
}
