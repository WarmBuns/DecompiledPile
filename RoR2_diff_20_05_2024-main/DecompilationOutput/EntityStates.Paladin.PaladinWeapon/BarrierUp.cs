using RoR2;

namespace EntityStates.Paladin.PaladinWeapon;

public class BarrierUp : BaseState
{
	public static float duration = 5f;

	public static string soundEffectString;

	private float stopwatch;

	private PaladinBarrierController paladinBarrierController;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(soundEffectString, base.gameObject);
		stopwatch = 0f;
		paladinBarrierController = GetComponent<PaladinBarrierController>();
		if ((bool)paladinBarrierController)
		{
			paladinBarrierController.EnableBarrier();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)paladinBarrierController)
		{
			paladinBarrierController.DisableBarrier();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= 0.1f && !base.inputBank.skill2.down && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
