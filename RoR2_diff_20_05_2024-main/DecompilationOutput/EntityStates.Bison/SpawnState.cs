using RoR2;
using UnityEngine;

namespace EntityStates.Bison;

public class SpawnState : BaseState
{
	public static GameObject spawnEffectPrefab;

	public static string spawnEffectMuzzle;

	public static float duration;

	public static float snowyOverlayDuration;

	public static Material snowyMaterial;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		EffectManager.SimpleMuzzleFlash(spawnEffectPrefab, base.gameObject, spawnEffectMuzzle, transmit: false);
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		Util.PlaySound("Play_bison_idle_graze", base.gameObject);
		Util.PlaySound("Play_bison_charge_attack_collide", base.gameObject);
		if ((bool)modelTransform)
		{
			CharacterModel component = modelTransform.GetComponent<CharacterModel>();
			if ((bool)component && (bool)snowyMaterial)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(component.gameObject);
				temporaryOverlayInstance.duration = snowyOverlayDuration;
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = snowyMaterial;
				temporaryOverlayInstance.inspectorCharacterModel = component;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.animateShaderAlpha = true;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
