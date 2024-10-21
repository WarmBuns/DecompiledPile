using RoR2;
using RoR2.HudOverlay;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Railgunner.Reload;

public class BoostConfirm : EntityState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public string enterSoundString;

	public OverlayController overlayController;

	private GenericSkill primarySkill;

	public SkillDef primaryOverride;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		if (overlayController == null)
		{
			return;
		}
		foreach (GameObject instances in overlayController.instancesList)
		{
			ActiveReloadBarController component = instances.GetComponent<ActiveReloadBarController>();
			if ((bool)component)
			{
				component.SetWasWindowHit(wasWindowHit: true);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new Boosted());
		}
	}

	public override void OnExit()
	{
		if (overlayController != null)
		{
			HudOverlayManager.RemoveOverlay(overlayController);
		}
		if ((bool)primarySkill)
		{
			primarySkill.UnsetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
		}
		base.OnExit();
	}

	public void OverridePrimary(GenericSkill skill, SkillDef overrideDef)
	{
		primarySkill = skill;
		primaryOverride = overrideDef;
		primarySkill.SetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
	}
}
