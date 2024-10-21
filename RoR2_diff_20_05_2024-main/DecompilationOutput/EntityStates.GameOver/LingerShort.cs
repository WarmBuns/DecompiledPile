using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class LingerShort : BaseGameOverControllerState
{
	private static readonly float duration = 3f;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
