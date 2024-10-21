using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Engi.SpiderMine;

public class BaseSpiderMineState : BaseState
{
	protected static int IdleToArmedStateHash = Animator.StringToHash("IdleToArmed");

	protected static int IdleToArmedParamHash = Animator.StringToHash("IdleToArmed.playbackRate");

	protected static int ArmedToChaseStateHash = Animator.StringToHash("ArmedToChase");

	protected static int ArmedToChaseParamHash = Animator.StringToHash("ArmedToChase.playbackRate");

	protected static int ChaseStateHash = Animator.StringToHash("Chase");

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string childLocatorStringToEnable;

	protected ProjectileStickOnImpact projectileStickOnImpact { get; private set; }

	protected ProjectileTargetComponent projectileTargetComponent { get; private set; }

	protected ProjectileGhostController projectileGhostController { get; private set; }

	protected virtual bool shouldStick => false;

	public override void OnEnter()
	{
		base.OnEnter();
		projectileStickOnImpact = GetComponent<ProjectileStickOnImpact>();
		projectileTargetComponent = GetComponent<ProjectileTargetComponent>();
		projectileGhostController = base.projectileController.ghost;
		if ((bool)base.modelLocator && (bool)projectileGhostController)
		{
			base.modelLocator.modelBaseTransform = projectileGhostController.transform;
			base.modelLocator.modelTransform = base.modelLocator.modelBaseTransform.Find("mdlEngiSpiderMine");
			base.modelLocator.preserveModel = true;
			base.modelLocator.noCorpse = true;
		}
		if (projectileStickOnImpact.enabled != shouldStick)
		{
			projectileStickOnImpact.enabled = shouldStick;
		}
		Transform transform = FindModelChild(childLocatorStringToEnable);
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: true);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	protected void EmitDustEffect()
	{
		if ((bool)projectileGhostController)
		{
			projectileGhostController.transform.Find("Ring").GetComponent<ParticleSystem>().Play();
		}
	}
}
