using RoR2;
using UnityEngine;

namespace EntityStates.Heretic;

public class SpawnState : BaseState
{
	public static float duration = 4f;

	public static string spawnSoundString;

	public static GameObject effectPrefab;

	public static Material overlayMaterial;

	public static float overlayDuration;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	private static int SpawnParamHash = Animator.StringToHash("Spawn.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(spawnSoundString, base.gameObject);
		EffectManager.SimpleEffect(effectPrefab, base.characterBody.corePosition, Quaternion.identity, transmit: false);
		PlayAnimation("Body", SpawnStateHash, SpawnParamHash, duration);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			CharacterModel component = modelTransform.GetComponent<CharacterModel>();
			if ((bool)component && (bool)overlayMaterial)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(component.gameObject);
				temporaryOverlayInstance.duration = overlayDuration;
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = overlayMaterial;
				temporaryOverlayInstance.inspectorCharacterModel = component;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.animateShaderAlpha = true;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
