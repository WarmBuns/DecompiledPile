using UnityEngine;

namespace EntityStates.AurelioniteHeart;

public class AurelioniteHeartInertState : AurelioniteHeartEntityState
{
	[SerializeField]
	private float animationDuration = 1f;

	[SerializeField]
	private string heartInertAnimationState;

	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		heartMovementTimer -= GetDeltaTime();
		if (heartMovementTimer <= 0f)
		{
			heartMovementTimer += 0.02f;
			if (base.gameObject.transform.position != inertPosition)
			{
				base.gameObject.transform.position += new Vector3(0f, -0.25f, 0f);
				return;
			}
			animator.StopPlayback();
			heartController.rebirthShrine.SetActive(value: true);
			outer.SetNextState(new AurelioniteHeartIdleState());
		}
	}
}
