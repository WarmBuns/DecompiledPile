using RoR2;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class Lightning1SpawnState : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject spawnEffect;

	public static float vfxSize = 6f;

	private bool playedSFX;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound("Play_boss_falseson_spawn", base.gameObject);
		PlayAnimation("Body", "Phase2Spawn", "StepBrothersPrep.playbackRate", baseDuration);
		PlayAnimation("FullBody, Override", "BufferEmpty");
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("FalseSonBoss/FalseSonBossLightningNovaSpawn"), new EffectData
		{
			origin = base.transform.position,
			scale = vfxSize
		}, transmit: true);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > 1f && !playedSFX)
		{
			Util.PlaySound("Play_boss_falseson_VO_anger", base.gameObject);
			playedSFX = true;
		}
		if (base.fixedAge > baseDuration)
		{
			outer.SetNextStateToMain();
		}
	}
}
