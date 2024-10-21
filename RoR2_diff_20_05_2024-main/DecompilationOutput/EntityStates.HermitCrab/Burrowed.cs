namespace EntityStates.HermitCrab;

public class Burrowed : BaseState
{
	public static float mortarCooldown;

	public float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Body", "Burrowed", 0.1f);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			if (base.inputBank.moveVector.sqrMagnitude > 0.1f)
			{
				outer.SetNextState(new BurrowOut());
			}
			if (base.fixedAge >= duration && base.inputBank.skill1.down)
			{
				outer.SetNextState(new FireMortar());
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
