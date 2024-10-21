using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Huntress;

public class AimArrowSnipe : BaseArrowBarrage
{
	public static SkillDef primarySkillDef;

	public static GameObject crosshairOverridePrefab;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private GenericSkill primarySkillSlot;

	private AimAnimator modelAimAnimator;

	public override void OnEnter()
	{
		base.OnEnter();
		modelAimAnimator = GetModelTransform().GetComponent<AimAnimator>();
		if ((bool)modelAimAnimator)
		{
			modelAimAnimator.enabled = true;
		}
		primarySkillSlot = (base.skillLocator ? base.skillLocator.primary : null);
		if ((bool)primarySkillSlot)
		{
			primarySkillSlot.SetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}
		PlayCrossfade("Body", "ArrowBarrageLoop", 0.1f);
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
	}

	protected override void HandlePrimaryAttack()
	{
		if ((bool)primarySkillSlot)
		{
			primarySkillSlot.ExecuteIfReady();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
		if (!primarySkillSlot || primarySkillSlot.stock == 0)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if ((bool)primarySkillSlot)
		{
			primarySkillSlot.UnsetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}
		crosshairOverrideRequest?.Dispose();
		base.OnExit();
	}
}
