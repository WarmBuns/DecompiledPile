using System;
using HG;
using UnityEngine;

namespace RoR2.Navigation;

public class NodeGraphNavigationSystem : BroadNavigationSystem
{
	public struct AgentData
	{
		public NodeGraph nodeGraph;

		public PathFollower pathFollower;

		public NodeGraph.PathRequest pathRequest;

		public HullClassification hullClassification;

		public PathTask pathTask;

		public float updateInterval;

		public float nextUpdate;

		public bool targetReachable;

		public float lastTargetReachableUpdate;

		public NodeGraph.NodeIndex? lastKnownNode;

		public float timeLastKnownNodeEncountered;

		public float estimatedArrivalTimeAtLastKnownNode;

		private PathFollower.Drawer pathDrawer;

		public bool drawPath
		{
			get
			{
				return pathDrawer != null;
			}
			set
			{
				if (drawPath != value)
				{
					if (!value)
					{
						pathDrawer.Dispose();
						pathDrawer = null;
					}
					else
					{
						pathDrawer = new PathFollower.Drawer(pathFollower);
					}
				}
			}
		}

		public void Initialize()
		{
			nextUpdate = float.NegativeInfinity;
			nodeGraph = null;
			updateInterval = 2f;
			hullClassification = HullClassification.Human;
			pathFollower = pathFollower ?? new PathFollower();
			pathRequest = pathRequest ?? new NodeGraph.PathRequest();
			targetReachable = false;
			lastTargetReachableUpdate = float.NegativeInfinity;
			lastKnownNode = null;
			timeLastKnownNodeEncountered = float.NegativeInfinity;
			estimatedArrivalTimeAtLastKnownNode = float.NegativeInfinity;
			drawPath = false;
		}

		public void Dispose()
		{
			drawPath = false;
			nodeGraph = null;
			nextUpdate = float.NegativeInfinity;
			pathFollower.Reset();
			pathRequest.Reset();
			pathTask = null;
		}

		public void InvalidatePath()
		{
			nextUpdate = float.NegativeInfinity;
			pathTask = null;
		}
	}

	public new struct Agent
	{
		private readonly BroadNavigationSystem.Agent inner;

		private NodeGraphNavigationSystem system => (NodeGraphNavigationSystem)inner.system;

		private ref AgentData agentData => ref system.allAgentData[(int)inner.handle];

		public NodeGraph nodeGraph
		{
			get
			{
				return agentData.nodeGraph;
			}
			set
			{
				agentData.nodeGraph = value;
			}
		}

		public HullClassification hullClassification
		{
			get
			{
				return agentData.hullClassification;
			}
			set
			{
				agentData.hullClassification = value;
			}
		}

		public bool drawPath
		{
			get
			{
				return agentData.drawPath;
			}
			set
			{
				agentData.drawPath = value;
			}
		}

		public PathFollower pathFollower
		{
			get
			{
				return agentData.pathFollower;
			}
			set
			{
				agentData.pathFollower = value;
			}
		}

		public Agent(BroadNavigationSystem.Agent inner)
		{
			this.inner = inner;
		}

		public void InvalidatePath()
		{
			agentData.InvalidatePath();
		}

		public void DebugDrawPath(Color color, float duration)
		{
			agentData.pathFollower.DebugDrawPath(color, duration);
		}

		public static explicit operator Agent(BroadNavigationSystem.Agent other)
		{
			return new Agent(other);
		}
	}

	private AgentData[] allAgentData = Array.Empty<AgentData>();

	private int maxAgentUpdatesPerFrame = 1;

	private int remainingAgentUpdateBudget;

	public new Agent GetAgent(AgentHandle agentHandle)
	{
		return new Agent(base.GetAgent(agentHandle));
	}

	protected override void CreateAgent(in AgentHandle index)
	{
		ArrayUtils.EnsureCapacity(ref allAgentData, base.agentCount);
		allAgentData[(int)index].Initialize();
	}

	protected override void DestroyAgent(in AgentHandle index)
	{
		allAgentData[(int)index].Dispose();
	}

	protected override void UpdateAgents(float deltaTime)
	{
		remainingAgentUpdateBudget = maxAgentUpdatesPerFrame;
		for (AgentHandle agentHandle = (AgentHandle)0; (int)agentHandle < allAgentData.Length; agentHandle++)
		{
			UpdateAgent(in agentHandle);
		}
	}

	protected override void ConfigureAgentFromBody(in AgentHandle index, CharacterBody body)
	{
		ref AgentData reference = ref allAgentData[(int)index];
		if ((bool)body)
		{
			reference.hullClassification = body.hullClassification;
			reference.nodeGraph = (SceneInfo.instance ? SceneInfo.instance.GetNodeGraph(body.isFlying ? MapNodeGroup.GraphType.Air : MapNodeGroup.GraphType.Ground) : null);
		}
		else
		{
			reference.hullClassification = HullClassification.Human;
			reference.nodeGraph = null;
			reference.InvalidatePath();
		}
	}

	protected override void InvalidateAgentPath(in AgentHandle index)
	{
		allAgentData[(int)index].InvalidatePath();
	}

	private void UpdateAgent(in AgentHandle agentHandle)
	{
		bool flag = false;
		ref readonly BaseAgentData agentData = ref GetAgentData(in agentHandle);
		if (!agentData.enabled)
		{
			return;
		}
		ref AgentData reference = ref allAgentData[(int)agentHandle];
		if (reference.pathTask == null && reference.nextUpdate <= localTime && remainingAgentUpdateBudget != 0)
		{
			flag = true;
			remainingAgentUpdateBudget--;
			reference.nextUpdate = localTime + reference.updateInterval;
			if (reference.lastKnownNode != reference.pathFollower.nextNode)
			{
				reference.lastKnownNode = reference.pathFollower.nextNode;
				reference.timeLastKnownNodeEncountered = localTime;
				Vector3? nextPosition = reference.pathFollower.GetNextPosition();
				if (nextPosition.HasValue && agentData.currentPosition.HasValue && agentData.maxWalkSpeed > 0f)
				{
					reference.estimatedArrivalTimeAtLastKnownNode = localTime + (nextPosition.Value - agentData.currentPosition.Value).magnitude / agentData.maxWalkSpeed;
				}
				else
				{
					reference.estimatedArrivalTimeAtLastKnownNode = float.PositiveInfinity;
				}
			}
			if (agentData.currentPosition.HasValue && agentData.goalPosition.HasValue && (bool)reference.nodeGraph && reference.lastKnownNode.HasValue)
			{
				reference.pathRequest.startPos = agentData.currentPosition.Value;
				if (localTime <= reference.estimatedArrivalTimeAtLastKnownNode + 1f && reference.lastKnownNode.Value.nodeIndex != -1)
				{
					reference.nodeGraph.GetNodePosition(reference.lastKnownNode.Value, out var position);
					float num = (agentData.maxWalkSpeed + reference.updateInterval) * 1.5f;
					float num2 = num * num;
					if ((position - agentData.currentPosition.Value).sqrMagnitude < num2 && !Physics.Linecast(agentData.currentPosition.Value, position + new Vector3(0f, 0.2f, 0f), out var _, LayerIndex.world.intVal, QueryTriggerInteraction.Ignore))
					{
						reference.pathRequest.startPos = reference.lastKnownNode.Value;
					}
				}
				reference.pathRequest.endPos = agentData.goalPosition.Value;
				reference.pathRequest.hullClassification = reference.hullClassification;
				reference.pathRequest.maxJumpHeight = agentData.maxJumpHeight;
				reference.pathRequest.maxSlope = agentData.maxSlopeAngle;
				reference.pathRequest.maxSpeed = agentData.maxWalkSpeed;
				reference.pathRequest.path = new Path(reference.nodeGraph);
				reference.pathTask = reference.nodeGraph.ComputePath(reference.pathRequest);
			}
		}
		if (reference.pathTask != null && (flag || remainingAgentUpdateBudget != 0))
		{
			if (!flag)
			{
				remainingAgentUpdateBudget--;
				flag = true;
			}
			if (reference.pathTask.status == PathTask.TaskStatus.Complete)
			{
				reference.pathFollower.SetPath(reference.pathTask.path);
				reference.lastTargetReachableUpdate = localTime;
				reference.targetReachable = reference.pathTask.wasReachable;
				reference.pathTask = null;
			}
		}
		if (agentData.currentPosition.HasValue)
		{
			reference.pathFollower.UpdatePosition(agentData.currentPosition.Value);
		}
		Vector3? nextPosition2 = reference.pathFollower.GetNextPosition();
		if (reference.targetReachable && !nextPosition2.HasValue)
		{
			nextPosition2 = agentData.goalPosition;
		}
		AgentOutput agentOutput = default(AgentOutput);
		agentOutput.nextPosition = nextPosition2;
		agentOutput.desiredJumpVelocity = (reference.pathFollower.nextWaypointNeedsJump ? reference.pathFollower.CalculateJumpVelocityNeededToReachNextWaypoint(agentData.maxWalkSpeed) : 0f);
		agentOutput.targetReachable = reference.targetReachable;
		agentOutput.lastPathUpdate = reference.lastTargetReachableUpdate;
		AgentOutput output = agentOutput;
		SetAgentOutput(in agentHandle, in output);
	}
}
