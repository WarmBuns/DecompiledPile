using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace EntityStates.AI.Walker;

public class LookBusy : BaseAIState
{
	private const float minDuration = 2f;

	private const float maxDuration = 7f;

	private Vector3 targetPosition;

	private float duration;

	private float lookTimer;

	private const float minLookDuration = 0.5f;

	private const float maxLookDuration = 4f;

	private const int lookTries = 4;

	private const float lookRaycastLength = 25f;

	protected virtual void PickNewTargetLookDirection()
	{
		if ((bool)base.bodyInputBank)
		{
			float num = 0f;
			Vector3 aimOrigin = base.bodyInputBank.aimOrigin;
			for (int i = 0; i < 4; i++)
			{
				Vector3 onUnitSphere = Random.onUnitSphere;
				float num2 = 25f;
				if (Physics.Raycast(new Ray(aimOrigin, onUnitSphere), out var hitInfo, 25f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					num2 = hitInfo.distance;
				}
				if (num2 > num)
				{
					num = num2;
					bodyInputs.desiredAimDirection = onUnitSphere;
				}
			}
		}
		lookTimer = Random.Range(0.5f, 4f);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = Random.Range(2f, 7f);
		base.bodyInputBank.moveVector = Vector3.zero;
		base.bodyInputBank.jump.PushState(newState: false);
		PickNewTargetLookDirection();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.ai || !base.body)
		{
			return;
		}
		if ((bool)base.ai.skillDriverEvaluation.dominantSkillDriver)
		{
			outer.SetNextState(new Combat());
		}
		if (base.ai.hasAimConfirmation)
		{
			lookTimer -= GetDeltaTime();
			if (lookTimer <= 0f)
			{
				PickNewTargetLookDirection();
			}
		}
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new Wander());
		}
	}

	public override BaseAI.BodyInputs GenerateBodyInputs(in BaseAI.BodyInputs previousBodyInputs)
	{
		return bodyInputs;
	}
}
