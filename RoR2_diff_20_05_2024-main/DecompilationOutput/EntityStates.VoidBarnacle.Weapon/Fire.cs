using UnityEngine;

namespace EntityStates.VoidBarnacle.Weapon;

public class Fire : GenericProjectileBaseState
{
	[SerializeField]
	public int numberOfFireballs;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateName;

	private float _interFireballDuration;

	private float _animationDuration;

	private Transform muzzleTransform;

	public override void OnEnter()
	{
		duration = baseDuration / attackSpeedStat;
		_interFireballDuration = duration / (float)numberOfFireballs;
		_animationDuration = _interFireballDuration;
		muzzleTransform = FindModelChild(targetMuzzle);
		base.OnEnter();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (stopwatch >= _animationDuration && numberOfFireballs > 0)
		{
			_animationDuration += _animationDuration;
			PlayAnimation(_animationDuration);
		}
	}

	protected override void PlayAnimation(float duration)
	{
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateName, _interFireballDuration);
	}

	protected override void FireProjectile()
	{
		base.FireProjectile();
		if (numberOfFireballs > 1)
		{
			firedProjectile = false;
			delayBeforeFiringProjectile += _interFireballDuration;
		}
		numberOfFireballs--;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
