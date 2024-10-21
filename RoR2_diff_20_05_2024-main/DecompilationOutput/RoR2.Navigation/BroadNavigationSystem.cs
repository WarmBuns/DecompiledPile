using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2.Navigation;

public abstract class BroadNavigationSystem : IDisposable
{
	public enum AgentHandle
	{
		None = -1
	}

	protected struct BaseAgentData
	{
		public bool enabled;

		public bool isValid;

		public float localTime;

		public Vector3? currentPosition;

		public Vector3? goalPosition;

		public float maxWalkSpeed;

		public float maxSlopeAngle;

		public float maxJumpHeight;
	}

	public struct AgentOutput
	{
		public float desiredJumpVelocity { get; set; }

		public Vector3? nextPosition { get; set; }

		public float lastPathUpdate { get; set; }

		public bool targetReachable { get; set; }

		public float time { get; set; }
	}

	public readonly struct Agent
	{
		public static readonly Agent invalid = new Agent(null, AgentHandle.None);

		public readonly BroadNavigationSystem system;

		public readonly AgentHandle handle;

		private ref BaseAgentData agentData => ref system.allAgentData[(int)handle];

		public bool enabled
		{
			get
			{
				return agentData.enabled;
			}
			set
			{
				agentData.enabled = value;
			}
		}

		public float maxSlopeAngle
		{
			get
			{
				return agentData.maxSlopeAngle;
			}
			set
			{
				agentData.maxSlopeAngle = value;
			}
		}

		public float maxWalkSpeed
		{
			get
			{
				return agentData.maxWalkSpeed;
			}
			set
			{
				agentData.maxWalkSpeed = value;
			}
		}

		public float maxJumpHeight
		{
			get
			{
				return agentData.maxJumpHeight;
			}
			set
			{
				agentData.maxJumpHeight = value;
			}
		}

		public Vector3? currentPosition
		{
			get
			{
				return agentData.currentPosition;
			}
			set
			{
				agentData.currentPosition = value;
			}
		}

		public Vector3? goalPosition
		{
			get
			{
				return agentData.goalPosition;
			}
			set
			{
				agentData.goalPosition = value;
			}
		}

		public AgentOutput output => system.agentOutputs[(int)handle];

		public Agent(BroadNavigationSystem system, AgentHandle handle)
		{
			this.handle = handle;
			this.system = system;
		}

		public void ConfigureFromBody(CharacterBody body)
		{
			system.ConfigureAgentFromBodyInternal(in handle, body);
		}

		public void InvalidatePath()
		{
			system.InvalidateAgentPathInternal(in handle);
		}
	}

	private static readonly List<BroadNavigationSystem> instancesList;

	private IndexAllocator agentIndexAllocator = new IndexAllocator();

	private BaseAgentData[] allAgentData = Array.Empty<BaseAgentData>();

	private AgentOutput[] agentOutputs = Array.Empty<AgentOutput>();

	protected float localTime;

	protected int agentCount { get; private set; }

	static BroadNavigationSystem()
	{
		instancesList = new List<BroadNavigationSystem>();
		RoR2Application.onFixedUpdate += StaticUpdate;
	}

	private static void StaticUpdate()
	{
		if (Time.fixedDeltaTime == 0f)
		{
			return;
		}
		foreach (BroadNavigationSystem instances in instancesList)
		{
			instances.Update(Time.fixedDeltaTime);
		}
	}

	public BroadNavigationSystem()
	{
		instancesList.Add(this);
	}

	public virtual void Dispose()
	{
		instancesList.Remove(this);
		for (AgentHandle index = (AgentHandle)0; (int)index < allAgentData.Length; index++)
		{
			if (allAgentData[(int)index].isValid)
			{
				DestroyAgentInternal(in index);
			}
		}
	}

	public void RequestAgent(ref Agent agent)
	{
		AgentHandle index = (AgentHandle)agentIndexAllocator.RequestIndex();
		CreateAgentInternal(in index);
		agent = GetAgent(index);
	}

	public void ReturnAgent(ref Agent agent)
	{
		DestroyAgentInternal(in agent.handle);
		agentIndexAllocator.FreeIndex((int)agent.handle);
		agent = Agent.invalid;
	}

	public Agent GetAgent(AgentHandle agentHandle)
	{
		return new Agent(this, agentHandle);
	}

	public bool IsValidHandle(in AgentHandle handle)
	{
		if (!ArrayUtils.IsInBounds(allAgentData, (int)handle))
		{
			return false;
		}
		return allAgentData[(int)handle].isValid;
	}

	public void GetAgentOutput(in AgentHandle handle, out AgentOutput output)
	{
		output = agentOutputs[(int)handle];
	}

	private void Update(float deltaTime)
	{
		localTime += deltaTime;
		UpdateAgentsInternal(deltaTime);
	}

	private void CreateAgentInternal(in AgentHandle index)
	{
		int num = agentCount + 1;
		agentCount = num;
		ArrayUtils.EnsureCapacity(ref allAgentData, agentCount);
		ArrayUtils.EnsureCapacity(ref agentOutputs, agentCount);
		ref BaseAgentData reference = ref allAgentData[(int)index];
		reference.isValid = true;
		reference.localTime = 0f;
		CreateAgent(in index);
	}

	private void DestroyAgentInternal(in AgentHandle index)
	{
		DestroyAgent(in index);
		allAgentData[(int)index] = default(BaseAgentData);
		agentOutputs[(int)index] = default(AgentOutput);
		int num = agentCount - 1;
		agentCount = num;
	}

	private void UpdateAgentsInternal(float deltaTime)
	{
		for (AgentHandle agentHandle = (AgentHandle)0; (int)agentHandle < allAgentData.Length; agentHandle++)
		{
			ref BaseAgentData reference = ref allAgentData[(int)agentHandle];
			if (reference.enabled)
			{
				reference.localTime += deltaTime;
			}
		}
		UpdateAgents(deltaTime);
	}

	private void ConfigureAgentFromBodyInternal(in AgentHandle index, CharacterBody body)
	{
		ref BaseAgentData reference = ref allAgentData[(int)index];
		reference.maxWalkSpeed = 0f;
		reference.maxSlopeAngle = 0f;
		reference.maxJumpHeight = 0f;
		reference.currentPosition = null;
		if ((bool)body)
		{
			reference.maxWalkSpeed = body.moveSpeed;
			reference.maxSlopeAngle = body.characterMotor?.Motor.MaxStableSlopeAngle ?? 0f;
			reference.maxJumpHeight = body.maxJumpHeight;
			reference.currentPosition = body.footPosition;
		}
		ConfigureAgentFromBody(in index, body);
	}

	private void InvalidateAgentPathInternal(in AgentHandle index)
	{
		InvalidateAgentPath(in index);
	}

	protected ref readonly BaseAgentData GetAgentData(in AgentHandle agentHandle)
	{
		return ref allAgentData[(int)agentHandle];
	}

	protected void SetAgentOutput(in AgentHandle agentHandle, in AgentOutput output)
	{
		ref AgentOutput reference = ref agentOutputs[(int)agentHandle];
		reference = output;
		reference.time = localTime;
	}

	protected abstract void CreateAgent(in AgentHandle index);

	protected abstract void DestroyAgent(in AgentHandle index);

	protected abstract void UpdateAgents(float deltaTime);

	protected abstract void ConfigureAgentFromBody(in AgentHandle index, CharacterBody body);

	protected abstract void InvalidateAgentPath(in AgentHandle index);
}
