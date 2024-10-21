using UnityEngine.Networking;

namespace EntityStates.Engi.Mine;

public class Arm : BaseMineState
{
	public static float duration;

	protected override bool shouldStick => true;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && duration <= base.fixedAge)
		{
			outer.SetNextState(new WaitForTarget());
		}
	}
}
