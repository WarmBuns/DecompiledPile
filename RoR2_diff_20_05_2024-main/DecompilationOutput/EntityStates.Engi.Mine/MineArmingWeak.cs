using UnityEngine.Networking;

namespace EntityStates.Engi.Mine;

public class MineArmingWeak : BaseMineArmingState
{
	public static float duration;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && duration <= base.fixedAge)
		{
			outer.SetNextState(new MineArmingFull());
		}
	}
}
