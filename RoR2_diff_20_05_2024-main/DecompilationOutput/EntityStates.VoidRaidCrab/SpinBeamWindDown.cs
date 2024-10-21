using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab;

public class SpinBeamWindDown : BaseSpinBeamAttackState
{
	public static AnimationCurve revolutionsCurve;

	public static string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= base.duration && base.isAuthority)
		{
			outer.SetNextState(new SpinBeamExit());
		}
		SetHeadYawRevolutions(revolutionsCurve.Evaluate(base.normalizedFixedAge));
	}
}
