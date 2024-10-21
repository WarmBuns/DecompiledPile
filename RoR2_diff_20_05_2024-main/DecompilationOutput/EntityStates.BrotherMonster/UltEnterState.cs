using RoR2;

namespace EntityStates.BrotherMonster;

public class UltEnterState : BaseState
{
	public static string soundString;

	public static float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Body", "UltEnter", "Ult.playbackRate", duration, 0.1f);
		Util.PlaySound(soundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			outer.SetNextState(new UltChannelState());
		}
	}
}
