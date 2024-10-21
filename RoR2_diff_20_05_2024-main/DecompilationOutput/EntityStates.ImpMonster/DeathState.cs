using RoR2;
using UnityEngine;

namespace EntityStates.ImpMonster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialEffect;

	public static GameObject deathEffect;

	private static float duration = 1.333f;

	private float stopwatch;

	private Animator animator;

	private bool hasPlayedDeathEffect;

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.enabled = false;
		}
		if ((bool)base.modelLocator && (bool)base.modelLocator.modelTransform.GetComponent<ChildLocator>() && (bool)initialEffect)
		{
			EffectManager.SimpleMuzzleFlash(initialEffect, base.gameObject, "Base", transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)animator)
		{
			stopwatch += GetDeltaTime();
			if (!hasPlayedDeathEffect && animator.GetFloat("DeathEffect") > 0.5f)
			{
				hasPlayedDeathEffect = true;
				EffectManager.SimpleMuzzleFlash(deathEffect, base.gameObject, "Center", transmit: false);
			}
			if (stopwatch >= duration)
			{
				EntityState.Destroy(base.gameObject);
			}
		}
	}
}
