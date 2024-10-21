using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates;

public class SpawnTeleporterState : BaseState
{
	private float duration = 4f;

	public static string soundString;

	public static float initialDelay;

	private bool hasTeleported;

	private Animator modelAnimator;

	private PrintController printController;

	private CharacterModel characterModel;

	private CameraTargetParams.AimRequest aimRequest;

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
		if ((bool)modelAnimator)
		{
			GameObject gameObject = modelAnimator.gameObject;
			characterModel = gameObject.GetComponent<CharacterModel>();
			characterModel.invisibilityCount++;
		}
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if (!hasTeleported)
		{
			characterModel.invisibilityCount--;
		}
		aimRequest?.Dispose();
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
			base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 3f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= initialDelay && !hasTeleported)
		{
			hasTeleported = true;
			characterModel.invisibilityCount--;
			duration = initialDelay;
			TeleportOutController.AddTPOutEffect(characterModel, 1f, 0f, duration);
			GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(base.gameObject);
			if ((bool)teleportEffectPrefab)
			{
				EffectManager.SimpleEffect(teleportEffectPrefab, base.transform.position, Quaternion.identity, transmit: false);
			}
			Util.PlaySound(soundString, base.gameObject);
		}
		if (base.fixedAge >= duration && hasTeleported && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
