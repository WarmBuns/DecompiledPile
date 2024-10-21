using UnityEngine.Networking;

namespace EntityStates.ArtifactShell;

public class WaitForIntro : ArtifactShellBaseState
{
	public static float baseDuration = 10f;

	private float duration;

	protected override bool interactionAvailable => false;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			outer.SetNextState(new WaitForKey());
		}
	}
}
