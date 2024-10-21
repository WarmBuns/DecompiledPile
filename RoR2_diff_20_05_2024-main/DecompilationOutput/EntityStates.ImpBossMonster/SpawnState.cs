using RoR2;
using UnityEngine;

namespace EntityStates.ImpBossMonster;

public class SpawnState : BaseState
{
	private float stopwatch;

	public static float duration = 4f;

	public static string spawnSoundString;

	public static GameObject spawnEffectPrefab;

	public static Material destealthMaterial;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Animator modelAnimator = GetModelAnimator();
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		Util.PlaySound(spawnSoundString, base.gameObject);
		if ((bool)spawnEffectPrefab)
		{
			EffectData effectData = new EffectData();
			effectData.origin = base.transform.position;
			EffectManager.SpawnEffect(spawnEffectPrefab, effectData, transmit: false);
		}
		if ((bool)destealthMaterial)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelAnimator.gameObject);
			temporaryOverlayInstance.duration = 1f;
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = destealthMaterial;
			temporaryOverlayInstance.inspectorCharacterModel = modelAnimator.gameObject.GetComponent<CharacterModel>();
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.animateShaderAlpha = true;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
