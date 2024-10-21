using EntityStates.Railgunner.Reload;
using RoR2;
using UnityEngine;

namespace EntityStates.Railgunner.Weapon;

public class ActiveReload : BaseState
{
	private const string reloadStateMachineName = "Reload";

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public string enterSoundString;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Reload");
		if ((bool)entityStateMachine && entityStateMachine.state is Reloading reloading)
		{
			reloading.AttemptBoost();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}
