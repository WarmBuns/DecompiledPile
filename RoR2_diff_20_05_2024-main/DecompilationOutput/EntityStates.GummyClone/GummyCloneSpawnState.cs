using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GummyClone;

public class GummyCloneSpawnState : BaseState
{
	[SerializeField]
	public float duration = 4f;

	[SerializeField]
	public string soundString;

	[SerializeField]
	public float initialDelay;

	[SerializeField]
	public GameObject effectPrefab;

	private bool hasFinished;

	private Animator modelAnimator;

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
		if (!hasFinished)
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
		if (base.fixedAge >= initialDelay && !hasFinished)
		{
			hasFinished = true;
			characterModel.invisibilityCount--;
			duration = initialDelay;
			if ((bool)effectPrefab)
			{
				EffectManager.SimpleEffect(effectPrefab, base.transform.position, Quaternion.identity, transmit: false);
			}
			Util.PlaySound(soundString, base.gameObject);
		}
		if (base.fixedAge >= duration && hasFinished && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
