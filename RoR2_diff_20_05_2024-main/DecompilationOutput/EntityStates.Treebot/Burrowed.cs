using EntityStates.Treebot.Weapon;
using RoR2;
using UnityEngine;

namespace EntityStates.Treebot;

public class Burrowed : GenericCharacterMain
{
	public static float mortarCooldown;

	public static string primarySkillName;

	public static string altPrimarySkillName;

	public static string utilitySkillName;

	public static string altUtilitySkillName;

	public float duration;

	private ChildLocator childLocator;

	private CameraTargetParams.AimRequest aimRequest;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Body", "Burrowed", 0.1f);
		base.skillLocator.primary = base.skillLocator.FindSkill(altPrimarySkillName);
		base.skillLocator.utility = base.skillLocator.FindSkill(altUtilitySkillName);
		base.skillLocator.primary.stateMachine.mainStateType = new SerializableEntityStateType(typeof(AimMortar));
		base.skillLocator.primary.stateMachine.SetNextStateToMain();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
		}
		if ((bool)childLocator)
		{
			base.characterBody.aimOriginTransform = childLocator.FindChild("AimOriginMortar");
		}
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
	}

	public override void OnExit()
	{
		aimRequest?.Dispose();
		base.skillLocator.primary = base.skillLocator.FindSkill(primarySkillName);
		base.skillLocator.utility = base.skillLocator.FindSkill(utilitySkillName);
		base.skillLocator.primary.stateMachine.mainStateType = new SerializableEntityStateType(typeof(Idle));
		base.skillLocator.primary.stateMachine.SetNextStateToMain();
		if ((bool)childLocator)
		{
			base.characterBody.aimOriginTransform = childLocator.FindChild("AimOriginSyringe");
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
