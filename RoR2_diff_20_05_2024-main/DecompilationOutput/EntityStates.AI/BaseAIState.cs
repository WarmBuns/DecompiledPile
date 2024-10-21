using System.Collections.Generic;
using RoR2;
using RoR2.CharacterAI;
using RoR2.ConVar;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.AI;

public abstract class BaseAIState : EntityState
{
	protected static FloatConVar cvAIUpdateInterval = new FloatConVar("ai_update_interval", ConVarFlags.Cheat, "0.2", "Frequency that the local navigator refreshes.");

	protected BaseAI.BodyInputs bodyInputs;

	protected bool isInJump;

	protected Vector3? jumpLockedMoveVector;

	protected CharacterMaster characterMaster { get; private set; }

	protected BaseAI ai { get; private set; }

	protected CharacterBody body { get; private set; }

	protected Transform bodyTransform { get; private set; }

	protected InputBankTest bodyInputBank { get; private set; }

	protected CharacterMotor bodyCharacterMotor { get; private set; }

	protected SkillLocator bodySkillLocator { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		characterMaster = GetComponent<CharacterMaster>();
		ai = GetComponent<BaseAI>();
		if ((bool)ai)
		{
			body = ai.body;
			bodyTransform = ai.bodyTransform;
			bodyInputBank = ai.bodyInputBank;
			bodyCharacterMotor = ai.bodyCharacterMotor;
			bodySkillLocator = ai.bodySkillLocator;
		}
		bodyInputs = default(BaseAI.BodyInputs);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
	}

	public virtual BaseAI.BodyInputs GenerateBodyInputs(in BaseAI.BodyInputs previousBodyInputs)
	{
		return bodyInputs;
	}

	protected void ModifyInputsForJumpIfNeccessary(ref BaseAI.BodyInputs bodyInputs)
	{
		if (!ai)
		{
			return;
		}
		BroadNavigationSystem.AgentOutput output = ai.broadNavigationAgent.output;
		bodyInputs.pressJump = false;
		if (!bodyCharacterMotor)
		{
			return;
		}
		bool isGrounded = bodyCharacterMotor.isGrounded;
		bool flag = isGrounded || bodyCharacterMotor.isFlying || !bodyCharacterMotor.useGravity;
		if (isInJump && flag)
		{
			isInJump = false;
			jumpLockedMoveVector = null;
		}
		if (isGrounded)
		{
			float num = Mathf.Max(output.desiredJumpVelocity, ai.localNavigator.jumpSpeed);
			if (num > 0f && body.jumpPower > 0f)
			{
				bool num2 = output.desiredJumpVelocity >= ai.localNavigator.jumpSpeed;
				num = body.jumpPower;
				bodyInputs.pressJump = true;
				if (num2 && output.nextPosition.HasValue)
				{
					Vector3 vector = output.nextPosition.Value - bodyTransform.position;
					Vector3 vector2 = vector;
					vector2.y = 0f;
					float num3 = Trajectory.CalculateFlightDuration(0f, vector.y, num);
					float walkSpeed = bodyCharacterMotor.walkSpeed;
					if (num3 > 0f && walkSpeed > 0f)
					{
						float magnitude = vector2.magnitude;
						float num4 = Mathf.Max(magnitude / num3 / bodyCharacterMotor.walkSpeed, 0f);
						jumpLockedMoveVector = vector2 * (num4 / magnitude);
						bodyCharacterMotor.moveDirection = jumpLockedMoveVector.Value;
					}
				}
				isInJump = true;
			}
		}
		if (jumpLockedMoveVector.HasValue)
		{
			bodyInputs.moveVector = jumpLockedMoveVector.Value;
		}
	}

	protected Vector3? PickRandomNearbyReachablePosition()
	{
		if (!ai || !body)
		{
			return null;
		}
		NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(body.isFlying ? MapNodeGroup.GraphType.Air : MapNodeGroup.GraphType.Ground);
		NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(nodeGraph, (HullMask)(1 << (int)body.hullClassification));
		NodeGraph.NodeIndex nodeIndex = nodeGraph.FindClosestNode(bodyTransform.position, body.hullClassification, 50f);
		nodeGraphSpider.AddNodeForNextStep(nodeIndex);
		nodeGraphSpider.PerformStep();
		nodeGraphSpider.PerformStep();
		List<NodeGraphSpider.StepInfo> collectedSteps = nodeGraphSpider.collectedSteps;
		if (collectedSteps.Count == 0)
		{
			return null;
		}
		int index = Random.Range(0, collectedSteps.Count);
		NodeGraph.NodeIndex node = collectedSteps[index].node;
		if (nodeGraph.GetNodePosition(node, out var position))
		{
			return position;
		}
		return null;
	}

	protected void AimAt(ref BaseAI.BodyInputs dest, BaseAI.Target aimTarget)
	{
		if (aimTarget != null && aimTarget.GetBullseyePosition(out var position))
		{
			dest.desiredAimDirection = (position - bodyInputBank.aimOrigin).normalized;
		}
	}

	protected void AimInDirection(ref BaseAI.BodyInputs dest, Vector3 aimDirection)
	{
		if (aimDirection != Vector3.zero)
		{
			dest.desiredAimDirection = aimDirection;
		}
	}
}
