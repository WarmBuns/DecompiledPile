using System;
using System.Collections.Generic;
using EntityStates;
using EntityStates.VoidRaidCrab;
using UnityEngine;

namespace RoR2.VoidRaidCrab;

[RequireComponent(typeof(CharacterBody))]
public class CentralLegController : MonoBehaviour
{
	public class SuppressBreaksRequest : IDisposable
	{
		private Action<SuppressBreaksRequest> onDispose;

		public SuppressBreaksRequest(Action<SuppressBreaksRequest> onDispose)
		{
			this.onDispose = onDispose;
		}

		public void Dispose()
		{
			onDispose?.Invoke(this);
		}
	}

	private class LegSupportTracker
	{
		public bool allLegsNeededForAnimation;

		private LegController[] legControllers;

		private bool[] currentLegSupportStates;

		private int halfLegsCount;

		public readonly FixedSizeArrayPool<bool> legBoolsPool;

		public LegSupportTracker(LegController[] legControllers)
		{
			this.legControllers = legControllers;
			legBoolsPool = new FixedSizeArrayPool<bool>(legControllers.Length);
			currentLegSupportStates = new bool[legControllers.Length];
			halfLegsCount = legControllers.Length / 2;
		}

		public void Refresh()
		{
			for (int i = 0; i < legControllers.Length; i++)
			{
				currentLegSupportStates[i] = legControllers[i].IsSupportingWeight();
			}
		}

		public bool IsLegStateStable()
		{
			return IsLegStateStable(currentLegSupportStates);
		}

		public bool IsLegStateStable(bool[] legSupportStates)
		{
			int num = 0;
			for (int i = 0; i < legControllers.Length; i++)
			{
				if (legSupportStates[i])
				{
					num++;
				}
			}
			return num > halfLegsCount;
		}

		public bool IsLegNeededForSupport(int legIndex)
		{
			if (allLegsNeededForAnimation)
			{
				return true;
			}
			bool[] array = legBoolsPool.Request();
			Array.Copy(currentLegSupportStates, array, currentLegSupportStates.Length);
			array[legIndex] = false;
			bool result = !IsLegStateStable(array);
			legBoolsPool.Return(array);
			return result;
		}
	}

	[SerializeField]
	private LegController[] legControllers;

	private CharacterBody body;

	private EntityStateMachine bodyStateMachine;

	private List<SuppressBreaksRequest> suppressBreaksRequests = new List<SuppressBreaksRequest>();

	private LegSupportTracker legSupportTracker;

	private int stompRequesterLegIndex = -1;

	private const bool useComplexCollapseCheck = false;

	private bool hasEffectiveAuthority
	{
		get
		{
			if ((bool)body)
			{
				return body.hasEffectiveAuthority;
			}
			return false;
		}
	}

	private void Awake()
	{
		body = GetComponent<CharacterBody>();
		bodyStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Body");
		legSupportTracker = new LegSupportTracker(legControllers);
		bodyStateMachine.nextStateModifier = ModifyBodyNextState;
	}

	private void FixedUpdate()
	{
		if (hasEffectiveAuthority)
		{
			UpdateLegsAuthority();
		}
	}

	private void ModifyBodyNextState(EntityStateMachine entityStateMachine, ref EntityState newNextState)
	{
		if (hasEffectiveAuthority && AreLegsBlockingBodyAnimation())
		{
			newNextState = new WaitForLegsAvailable
			{
				nextState = newNextState
			};
		}
	}

	private void UpdateLegsAuthority()
	{
		bool allLegsNeededForAnimation = !bodyStateMachine.IsInMainState();
		legSupportTracker.allLegsNeededForAnimation = allLegsNeededForAnimation;
		legSupportTracker.Refresh();
		if (bodyStateMachine.CanInterruptState(InterruptPriority.Pain))
		{
			bool flag = false;
			for (int i = 0; i < legControllers.Length; i++)
			{
				if (legControllers[i].IsBreakPending())
				{
					flag = true;
					legControllers[i].CompleteBreakAuthority();
				}
			}
			if (flag)
			{
				EntityState nextState = (legSupportTracker.IsLegStateStable() ? ((BaseState)new LegBreakStunState()) : ((BaseState)new Collapse()));
				bodyStateMachine.SetNextState(nextState);
			}
		}
		TryNextStompAuthority();
	}

	public bool AreLegsBlockingBodyAnimation()
	{
		for (int i = 0; i < legControllers.Length; i++)
		{
			if (legControllers[i].IsStomping())
			{
				return true;
			}
		}
		return false;
	}

	private void TryNextStompAuthority()
	{
	}

	private int WrapToRange(int value, int minInclusive, int maxExclusive)
	{
		if (maxExclusive <= minInclusive)
		{
			throw new ArgumentException("'max' must be greater than 'min'");
		}
		value -= minInclusive;
		int num = maxExclusive - minInclusive;
		int num2 = value % num;
		return minInclusive + ((num2 < 0) ? (num2 + num) : num2);
	}

	private LegController GetLegRingBuffered(int i)
	{
		return legControllers[WrapToRange(i, 0, legControllers.Length)];
	}

	private bool CheckLegsShouldCollapse()
	{
		int num = 0;
		int num2 = legControllers.Length / 2;
		for (int i = 0; i < legControllers.Length; i++)
		{
			if (legControllers[i].IsBroken())
			{
				num++;
				if (num >= num2)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool AreAnyBreaksPending()
	{
		LegController[] array = legControllers;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].IsBreakPending())
			{
				return true;
			}
		}
		return false;
	}

	public SuppressBreaksRequest SuppressBreaks()
	{
		if (suppressBreaksRequests.Count == 0)
		{
			LegController[] array = legControllers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].isBreakSuppressed = true;
			}
		}
		SuppressBreaksRequest suppressBreaksRequest = new SuppressBreaksRequest(RemoveSuppressBreaksRequest);
		suppressBreaksRequests.Add(suppressBreaksRequest);
		return suppressBreaksRequest;
	}

	public void RegenerateAllBrokenServer()
	{
		LegController[] array = legControllers;
		foreach (LegController legController in array)
		{
			if (legController.IsBroken())
			{
				legController.RegenerateServer();
			}
		}
	}

	private void RemoveSuppressBreaksRequest(SuppressBreaksRequest request)
	{
		suppressBreaksRequests.Remove(request);
		if (suppressBreaksRequests.Count == 0)
		{
			LegController[] array = legControllers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].isBreakSuppressed = false;
			}
		}
	}

	public bool IsBodyRelated(CharacterBody bodyToCheck)
	{
		if ((object)body == bodyToCheck)
		{
			return true;
		}
		LegController[] array = legControllers;
		foreach (LegController legController in array)
		{
			if ((object)bodyToCheck == legController.jointBody)
			{
				return true;
			}
		}
		return false;
	}
}
