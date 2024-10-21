using RoR2;
using UnityEngine;

namespace EntityStates.Huntress;

public class BaseArrowBarrage : BaseState
{
	[SerializeField]
	public float maxDuration;

	[SerializeField]
	public string beginLoopSoundString;

	[SerializeField]
	public string endLoopSoundString;

	[SerializeField]
	public string fireSoundString;

	private HuntressTracker huntressTracker;

	private CameraTargetParams.AimRequest aimRequest;

	private static int FireArrowRainStateHash = Animator.StringToHash("FireArrowRain");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginLoopSoundString, base.gameObject);
		huntressTracker = GetComponent<HuntressTracker>();
		if ((bool)huntressTracker)
		{
			huntressTracker.enabled = false;
		}
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity = Vector3.zero;
		}
		if (base.isAuthority && (bool)base.inputBank)
		{
			if ((bool)base.skillLocator && base.skillLocator.utility.IsReady() && base.inputBank.skill3.justPressed)
			{
				outer.SetNextStateToMain();
			}
			else if (base.fixedAge >= maxDuration || base.inputBank.skill1.justPressed || base.inputBank.skill4.justPressed)
			{
				HandlePrimaryAttack();
			}
		}
	}

	protected virtual void HandlePrimaryAttack()
	{
	}

	public override void OnExit()
	{
		PlayAnimation("FullBody, Override", FireArrowRainStateHash);
		Util.PlaySound(endLoopSoundString, base.gameObject);
		Util.PlaySound(fireSoundString, base.gameObject);
		aimRequest?.Dispose();
		if ((bool)huntressTracker)
		{
			huntressTracker.enabled = true;
		}
		base.OnExit();
	}
}
