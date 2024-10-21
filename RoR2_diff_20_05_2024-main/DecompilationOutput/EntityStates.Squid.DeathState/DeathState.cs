using UnityEngine;

namespace EntityStates.Squid.DeathState;

public class DeathState : BaseState
{
	public static float baseDuration;

	private float duration;

	private static int DeathStateHash = Animator.StringToHash("Death");

	public override void OnEnter()
	{
		duration = baseDuration;
		base.OnEnter();
		PlayAnimation("Body", DeathStateHash);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying && (bool)base.gameObject)
		{
			EntityState.Destroy(base.gameObject);
		}
		base.OnExit();
	}
}
