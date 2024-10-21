using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.Seeker;

public class BaseActive : BaseScopeState
{
	[SerializeField]
	public SkillDef primaryOverride;

	[SerializeField]
	public LoopSoundDef loopSound;

	private GenericSkill overriddenSkill;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	public override void OnEnter()
	{
		base.OnEnter();
		SetScopeAlpha(1f);
		StartScopeParamsOverride(0f);
		GenericSkill genericSkill = base.skillLocator?.primary;
		if ((bool)genericSkill)
		{
			TryOverrideSkill(genericSkill);
			genericSkill.onSkillChanged += TryOverrideSkill;
		}
		if (base.isAuthority)
		{
			loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, loopSound);
		}
	}

	public override void OnExit()
	{
		if (loopPtr.isValid)
		{
			LoopSoundManager.StopSoundLoopLocal(loopPtr);
		}
		GenericSkill genericSkill = base.skillLocator?.primary;
		if ((bool)genericSkill)
		{
			genericSkill.onSkillChanged -= TryOverrideSkill;
		}
		if ((bool)overriddenSkill)
		{
			overriddenSkill.UnsetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
		}
		EndScopeParamsOverride(0f);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && (!IsKeyDownAuthority() || base.characterBody.isSprinting))
		{
			outer.SetNextState(GetNextState());
		}
	}

	private void TryOverrideSkill(GenericSkill skill)
	{
		if ((bool)skill && !overriddenSkill && !skill.HasSkillOverrideOfPriority(GenericSkill.SkillOverridePriority.Contextual))
		{
			overriddenSkill = skill;
			overriddenSkill.SetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
			overriddenSkill.stock = base.skillLocator.secondary.stock;
		}
	}

	protected virtual BaseWindDown GetNextState()
	{
		return new BaseWindDown();
	}
}
