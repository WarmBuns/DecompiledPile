using RoR2;

namespace EntityStates.Scorchling;

public class SpawnState : EntityState
{
	public static float duration = 0.5f;

	public static string spawnSoundString;

	public static string burrowLoopSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(spawnSoundString, base.gameObject);
		Util.PlaySound(burrowLoopSoundString, base.gameObject);
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
