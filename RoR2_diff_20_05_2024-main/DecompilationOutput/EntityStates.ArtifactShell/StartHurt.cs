using RoR2;
using UnityEngine.Networking;

namespace EntityStates.ArtifactShell;

public class StartHurt : ArtifactShellBaseState
{
	public static float baseDuration = 0.25f;

	public static string firstHurtSound;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.healthComponent.combinedHealthFraction >= 1f)
		{
			Util.PlaySound(firstHurtSound, base.gameObject);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= baseDuration)
		{
			outer.SetNextState(new Hurt());
		}
	}
}
