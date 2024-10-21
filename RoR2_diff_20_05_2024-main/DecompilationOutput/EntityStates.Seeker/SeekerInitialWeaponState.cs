using RoR2;
using RoR2.Skills;
using RoR2.UI;

namespace EntityStates.Seeker;

public class SeekerInitialWeaponState : BaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		GenericSkill genericSkill = base.skillLocator.FindSkillByFamilyName("SeekerBodyPrimaryFamily");
		if (genericSkill == null)
		{
			outer.SetNextStateToMain();
			return;
		}
		SeekerWeaponSkillDef seekerWeaponSkillDef = genericSkill.skillDef as SeekerWeaponSkillDef;
		if (seekerWeaponSkillDef == null)
		{
			outer.SetNextStateToMain();
			return;
		}
		HandleCrosshair(seekerWeaponSkillDef);
		HandleTargetTracking(seekerWeaponSkillDef);
		outer.SetNextStateToMain();
	}

	private void HandleTargetTracking(SeekerWeaponSkillDef seekerWeapon)
	{
		HuntressTracker component = GetComponent<HuntressTracker>();
		if (!(component == null))
		{
			component.enabled = seekerWeapon.targetTrackingIndicator;
		}
	}

	private void HandleCrosshair(SeekerWeaponSkillDef seekerWeapon)
	{
		CrosshairUtils.RequestOverrideForBody(base.characterBody, seekerWeapon.crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
	}
}
