using RoR2;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class HeartSpawnState : BaseState
{
	[SerializeField]
	private float baseAnimDuration = 2f;

	[SerializeField]
	private float baseSpawnDuration = 4f;

	[SerializeField]
	private GameObject spawnEffect;

	[SerializeField]
	private float vfxSize = 6f;

	private bool playedSFX;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound("Play_boss_falseson_spawn", base.gameObject);
		Util.PlaySound("Play_boss_falseson_VO_anger", base.gameObject);
		PlayAnimation("Body", "Phase1Spawn", "StepBrothersPrep.playbackRate", baseAnimDuration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > 1f && !playedSFX)
		{
			Util.PlaySound("Play_boss_falseson_VO_anger", base.gameObject);
			playedSFX = true;
		}
		if (base.fixedAge > baseSpawnDuration)
		{
			outer.SetNextStateToMain();
		}
	}
}
