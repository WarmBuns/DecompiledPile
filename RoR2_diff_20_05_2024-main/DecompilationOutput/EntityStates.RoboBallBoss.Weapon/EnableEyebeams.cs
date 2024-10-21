using RoR2;

namespace EntityStates.RoboBallBoss.Weapon;

public class EnableEyebeams : BaseState
{
	public static float baseDuration;

	public static string soundString;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(soundString, base.gameObject);
		EntityStateMachine[] components = base.gameObject.GetComponents<EntityStateMachine>();
		foreach (EntityStateMachine entityStateMachine in components)
		{
			if (entityStateMachine.customName.Contains("EyeBeam"))
			{
				entityStateMachine.SetNextState(new FireSpinningEyeBeam());
			}
		}
	}
}
