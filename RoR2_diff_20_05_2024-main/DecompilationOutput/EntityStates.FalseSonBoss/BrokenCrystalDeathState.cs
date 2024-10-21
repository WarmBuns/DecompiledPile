using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSonBoss;

public class BrokenCrystalDeathState : GenericCharacterDeath
{
	public static float duration = 3f;

	public static GameObject deathEffectPrefab;

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
		MeridianEventTriggerInteraction.instance.musicPhaseTwo.SetActive(value: false);
		MeridianEventTriggerInteraction.instance.musicPhaseThree.SetActive(value: true);
		PlayAnimation("FullBody, Override", "Phase2Death", "StepBrothersPrep.playbackRate", duration);
	}

	public override void FixedUpdate()
	{
		if (base.fixedAge >= 2f && !playedSFX)
		{
			Util.PlaySound("Play_boss_falseson_phaseTransition_kneel", base.characterBody.gameObject);
			Util.PlaySound("Play_boss_falseson_VO_anger", base.characterBody.gameObject);
			playedSFX = true;
		}
		if (!_shouldAutoDestroy && NetworkServer.active && (double)base.fixedAge >= (double)duration + 0.5)
		{
			_shouldAutoDestroy = true;
			EffectManager.SpawnEffect(deathEffectPrefab, new EffectData
			{
				origin = base.characterBody.corePosition,
				scale = vfxSize
			}, transmit: true);
		}
		base.FixedUpdate();
	}

	public override void OnExit()
	{
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
