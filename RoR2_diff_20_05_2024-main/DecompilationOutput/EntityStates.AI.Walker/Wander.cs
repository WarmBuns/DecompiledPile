using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.AI.Walker;

public class Wander : BaseAIState
{
	private Vector3? targetPosition;

	private float lookTimer;

	private const float minLookDuration = 0.5f;

	private const float maxLookDuration = 4f;

	private const int lookTries = 1;

	private const float lookRaycastLength = 25f;

	private Vector3 targetLookPosition;

	private float aiUpdateTimer;

	public override void Reset()
	{
		base.Reset();
		targetPosition = Vector3.zero;
		lookTimer = 0f;
		targetLookPosition = Vector3.zero;
	}

	private void PickNewTargetLookPosition()
	{
		if ((bool)base.bodyInputBank)
		{
			float num = 0f;
			Vector3 aimOrigin = base.bodyInputBank.aimOrigin;
			Vector3 vector = base.bodyInputBank.moveVector;
			if (vector == Vector3.zero)
			{
				vector = Random.onUnitSphere;
			}
			for (int i = 0; i < 1; i++)
			{
				Vector3 direction = Util.ApplySpread(vector, 0f, 60f, 0f, 0f);
				float num2 = 25f;
				Ray ray = new Ray(aimOrigin, direction);
				if (Physics.Raycast(ray, out var hitInfo, 25f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					num2 = hitInfo.distance;
				}
				if (num2 > num)
				{
					num = num2;
					targetLookPosition = ray.GetPoint(num2);
				}
			}
		}
		lookTimer = Random.Range(0.5f, 4f);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.ai && (bool)base.body)
		{
			BroadNavigationSystem.Agent broadNavigationAgent = base.ai.broadNavigationAgent;
			targetPosition = PickRandomNearbyReachablePosition();
			if (targetPosition.HasValue)
			{
				broadNavigationAgent.goalPosition = targetPosition.Value;
				broadNavigationAgent.InvalidatePath();
			}
			PickNewTargetLookPosition();
			aiUpdateTimer = 0.5f;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		aiUpdateTimer -= GetDeltaTime();
		if (!base.ai || !base.body)
		{
			return;
		}
		if ((bool)base.ai.skillDriverEvaluation.dominantSkillDriver)
		{
			outer.SetNextState(new Combat());
		}
		base.ai.SetGoalPosition(targetPosition);
		_ = base.bodyTransform.position;
		BroadNavigationSystem.Agent broadNavigationAgent = base.ai.broadNavigationAgent;
		if (aiUpdateTimer <= 0f)
		{
			aiUpdateTimer = BaseAIState.cvAIUpdateInterval.value;
			base.ai.localNavigator.targetPosition = broadNavigationAgent.output.nextPosition ?? base.ai.localNavigator.targetPosition;
			base.ai.localNavigator.Update(BaseAIState.cvAIUpdateInterval.value);
			if ((bool)base.bodyInputBank)
			{
				bodyInputs.moveVector = base.ai.localNavigator.moveVector * 0.25f;
				bodyInputs.desiredAimDirection = (targetLookPosition - base.bodyInputBank.aimOrigin).normalized;
			}
			lookTimer -= GetDeltaTime();
			if (lookTimer <= 0f)
			{
				PickNewTargetLookPosition();
			}
			bool flag = false;
			if (targetPosition.HasValue)
			{
				float sqrMagnitude = (base.body.footPosition - targetPosition.Value).sqrMagnitude;
				float num = base.body.radius * base.body.radius * 4f;
				flag = sqrMagnitude > num;
			}
			if (!flag)
			{
				outer.SetNextState(new LookBusy());
			}
		}
	}

	public override BaseAI.BodyInputs GenerateBodyInputs(in BaseAI.BodyInputs previousBodyInputs)
	{
		ModifyInputsForJumpIfNeccessary(ref bodyInputs);
		return bodyInputs;
	}
}
