using UnityEngine;

namespace EntityStates.MagmaWorm;

public class Leap : BaseState
{
	private enum LeapState
	{
		Burrow,
		Ascend,
		Fall,
		Resurface
	}

	private Transform modelBaseTransform;

	private readonly float diveDepth = 200f;

	private readonly Vector3 idealDiveVelocity = Vector3.down * 90f;

	private readonly Vector3 idealLeapVelocity = Vector3.up * 90f;

	private float leapAcceleration = 80f;

	private float resurfaceSpeed = 60f;

	private Vector3 velocity;

	private LeapState leapState;

	public override void OnEnter()
	{
		base.OnEnter();
		modelBaseTransform = GetModelBaseTransform();
		leapState = LeapState.Burrow;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		switch (leapState)
		{
		case LeapState.Burrow:
			if ((bool)modelBaseTransform)
			{
				if (modelBaseTransform.position.y >= base.transform.position.y - diveDepth)
				{
					velocity = Vector3.MoveTowards(velocity, idealDiveVelocity, leapAcceleration * GetDeltaTime());
					modelBaseTransform.position += velocity * GetDeltaTime();
				}
				else
				{
					leapState = LeapState.Ascend;
				}
			}
			break;
		case LeapState.Ascend:
			if ((bool)modelBaseTransform)
			{
				if (modelBaseTransform.position.y <= base.transform.position.y)
				{
					velocity = Vector3.MoveTowards(velocity, idealLeapVelocity, leapAcceleration * GetDeltaTime());
					modelBaseTransform.position += velocity * GetDeltaTime();
				}
				else
				{
					leapState = LeapState.Fall;
				}
			}
			break;
		case LeapState.Fall:
			if ((bool)modelBaseTransform)
			{
				if (modelBaseTransform.position.y >= base.transform.position.y - diveDepth)
				{
					velocity += Physics.gravity * GetDeltaTime();
					modelBaseTransform.position += velocity * GetDeltaTime();
				}
				else
				{
					leapState = LeapState.Resurface;
				}
			}
			break;
		case LeapState.Resurface:
			velocity = Vector3.zero;
			modelBaseTransform.position = Vector3.MoveTowards(modelBaseTransform.position, base.transform.position, resurfaceSpeed * GetDeltaTime());
			if (modelBaseTransform.position.y >= base.transform.position.y)
			{
				outer.SetNextStateToMain();
			}
			break;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
