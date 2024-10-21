using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSonBoss;

public class CrystalDeathState : GenericCharacterDeath
{
	public static GameObject deathEffectPrefab;

	public static float duration = 3f;

	public float vfxSize = 8f;

	private bool playedSFX;

	private bool _shouldAutoDestroy;

	protected override bool shouldAutoDestroy => _shouldAutoDestroy;

	public override void OnEnter()
	{
		base.OnEnter();
	}

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayAnimation("FullBody, Override", "Phase1Death", "StepBrothersPrep.playbackRate", duration);
		MeridianEventTriggerInteraction.instance.musicPhaseOne.SetActive(value: false);
		MeridianEventTriggerInteraction.instance.musicPhaseTwo.SetActive(value: true);
	}

	public override void FixedUpdate()
	{
		if (base.fixedAge >= 2f && !playedSFX)
		{
			Util.PlaySound("Play_boss_falseson_phaseTransition_kneel", base.characterBody.gameObject);
			Util.PlaySound("Play_boss_falseson_VO_groan", base.gameObject);
			playedSFX = true;
		}
		if (!_shouldAutoDestroy && NetworkServer.active && base.fixedAge >= duration + 0.5f)
		{
			EffectManager.SpawnEffect(deathEffectPrefab, new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = vfxSize
			}, transmit: true);
			_shouldAutoDestroy = true;
		}
		base.FixedUpdate();
	}

	public override void OnExit()
	{
		DestroyBodyAsapServer();
		DestroyModel();
		base.OnExit();
		MeridianEventTriggerInteraction.FSBFPhaseBaseState.readyToSpawnNextBossBody = true;
		DestroyBodyServer();
	}

	private void DestroyBodyServer()
	{
		if (NetworkServer.active)
		{
			OnPreDestroyBodyServer();
			EntityState.Destroy(base.gameObject);
		}
	}
}
