using UnityEngine;

namespace EntityStates.GrandParentSun;

public class GrandParentSunSpawn : GrandParentSunBase
{
	public static float baseDuration;

	private float duration;

	protected override float desiredVfxScale => Mathf.Clamp01(base.age / duration);

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new GrandParentSunMain());
		}
	}
}
