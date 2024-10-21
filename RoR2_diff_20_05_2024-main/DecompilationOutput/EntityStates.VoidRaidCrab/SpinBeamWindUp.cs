using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab;

public class SpinBeamWindUp : BaseSpinBeamAttackState
{
	public static AnimationCurve revolutionsCurve;

	public static GameObject warningLaserPrefab;

	public static string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		CreateBeamVFXInstance(warningLaserPrefab);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= base.duration && base.isAuthority)
		{
			outer.SetNextState(new SpinBeamAttack());
		}
		SetHeadYawRevolutions(revolutionsCurve.Evaluate(base.normalizedFixedAge));
	}
}
