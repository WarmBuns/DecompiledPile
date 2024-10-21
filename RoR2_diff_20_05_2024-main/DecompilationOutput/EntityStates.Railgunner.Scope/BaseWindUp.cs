using RoR2;
using UnityEngine;

namespace EntityStates.Railgunner.Scope;

public class BaseWindUp : BaseScopeState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string enterSoundString;

	private float duration;

	public override void OnEnter()
	{
		duration = baseDuration;
		base.OnEnter();
		SetScopeAlpha(0f);
		StartScopeParamsOverride(duration);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void OnExit()
	{
		EndScopeParamsOverride(0f);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			BaseActive nextState = GetNextState();
			nextState.activatorSkillSlot = base.activatorSkillSlot;
			outer.SetNextState(nextState);
		}
	}

	public override void Update()
	{
		base.Update();
		SetScopeAlpha(Mathf.Clamp01(base.age / duration));
	}

	protected virtual BaseActive GetNextState()
	{
		return new BaseActive();
	}

	protected override float GetScopeEntryDuration()
	{
		return duration;
	}

	public override void ModifyNextState(EntityState nextState)
	{
	}
}
