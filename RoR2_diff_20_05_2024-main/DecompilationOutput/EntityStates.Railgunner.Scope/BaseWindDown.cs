using RoR2;
using UnityEngine;

namespace EntityStates.Railgunner.Scope;

public class BaseWindDown : BaseScopeState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string enterSoundString;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		SetScopeAlpha(1f);
		RemoveOverlay(duration);
		StartScopeParamsOverride(0f);
		EndScopeParamsOverride(duration);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void Update()
	{
		base.Update();
		SetScopeAlpha(1f - Mathf.Clamp01(base.age / duration));
	}

	protected override CharacterCameraParams GetCameraParams()
	{
		return null;
	}
}
