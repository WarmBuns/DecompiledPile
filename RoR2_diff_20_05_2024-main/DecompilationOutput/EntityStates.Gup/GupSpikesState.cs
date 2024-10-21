using RoR2;
using UnityEngine;

namespace EntityStates.Gup;

public class GupSpikesState : BasicMeleeAttack
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string playbackRateParam;

	[SerializeField]
	public float crossfadeDuration;

	[SerializeField]
	public string initialHitboxActiveParameter;

	[SerializeField]
	public string initialHitboxName;

	private HitBox initialHitBox;

	public override void OnEnter()
	{
		base.OnEnter();
		StartAimMode(0f);
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterDirection.forward;
		}
		if (!hitBoxGroup)
		{
			return;
		}
		HitBox[] hitBoxes = hitBoxGroup.hitBoxes;
		foreach (HitBox hitBox in hitBoxes)
		{
			if (hitBox.gameObject.name == initialHitboxName)
			{
				initialHitBox = hitBox;
				break;
			}
		}
	}

	protected override void PlayAnimation()
	{
		PlayCrossfade(animationLayerName, animationStateName, playbackRateParam, duration, crossfadeDuration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)initialHitBox)
		{
			initialHitBox.enabled = animator.GetFloat(initialHitboxActiveParameter) > 0.5f;
		}
	}
}
