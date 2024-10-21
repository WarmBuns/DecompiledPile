using RoR2;
using UnityEngine;

namespace EntityStates.Assassin2;

public class DeathState : GenericCharacterDeath
{
	public static GameObject deathEffectPrefab;

	public static float duration = 1.333f;

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
				EffectData effectData = new EffectData();
				effectData.origin = base.transform.position;
				EffectManager.SpawnEffect(deathEffectPrefab, effectData, transmit: false);
			}
			if (stopwatch >= duration)
			{
				DestroyModel();
				EntityState.Destroy(base.gameObject);
			}
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
