using System;
using EntityStates;
using JetBrains.Annotations;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(EntityStateMachine))]
[RequireComponent(typeof(PurchaseInteraction))]
public class RouletteChestController : NetworkBehaviour
{
	public struct Entry
	{
		public PickupIndex pickupIndex;

		public Run.FixedTimeStamp endTime;
	}

	private class RouletteChestControllerBaseState : EntityState
	{
		protected RouletteChestController rouletteChestController { get; private set; }

		public override void OnEnter()
		{
			base.OnEnter();
			rouletteChestController = GetComponent<RouletteChestController>();
		}

		public virtual void HandleInteractionServer(Interactor activator)
		{
		}
	}

	private class Idle : RouletteChestControllerBaseState
	{
		private static int IdleStateHash = Animator.StringToHash("Idle");

		public override void OnEnter()
		{
			base.OnEnter();
			PlayAnimation("Body", IdleStateHash);
			base.rouletteChestController.purchaseInteraction.Networkavailable = true;
		}

		public override void HandleInteractionServer(Interactor activator)
		{
			base.HandleInteractionServer(activator);
			outer.SetNextState(new Startup());
		}
	}

	private class Startup : RouletteChestControllerBaseState
	{
		public static float baseDuration;

		public static string soundEntryEvent;

		private static int IdleToActiveStateHash = Animator.StringToHash("IdleToActive");

		public override void OnEnter()
		{
			base.OnEnter();
			PlayAnimation("Body", IdleToActiveStateHash);
			base.rouletteChestController.purchaseInteraction.Networkavailable = false;
			base.rouletteChestController.purchaseInteraction.costType = CostTypeIndex.None;
			Util.PlaySound(soundEntryEvent, base.gameObject);
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge > baseDuration)
			{
				outer.SetNextState(new Cycling());
			}
		}
	}

	private class Cycling : RouletteChestControllerBaseState
	{
		public static string soundCycleEvent;

		public static string soundCycleSpeedRtpc;

		public static float soundCycleSpeedRtpcScale;

		private static int ActiveLoopStateHash = Animator.StringToHash("ActiveLoop");

		private static int ActiveLoopParamHash = Animator.StringToHash("ActiveLoop.playbackRate");

		public override void OnEnter()
		{
			base.OnEnter();
			base.rouletteChestController.onChangedEntryClient.AddListener(OnChangedEntryClient);
			if (NetworkServer.active)
			{
				base.rouletteChestController.BeginCycleServer();
				base.rouletteChestController.onCycleCompletedServer.AddListener(OnCycleCompleted);
			}
			base.rouletteChestController.purchaseInteraction.Networkavailable = true;
			base.rouletteChestController.purchaseInteraction.costType = CostTypeIndex.None;
		}

		private void OnCycleCompleted()
		{
			outer.SetNextState(new Opening());
		}

		public override void OnExit()
		{
			base.rouletteChestController.onCycleCompletedServer.RemoveListener(OnCycleCompleted);
			base.rouletteChestController.onChangedEntryClient.RemoveListener(OnChangedEntryClient);
			base.OnExit();
		}

		private void OnChangedEntryClient()
		{
			int entryIndexForTime = base.rouletteChestController.GetEntryIndexForTime(Run.FixedTimeStamp.now);
			float num = base.rouletteChestController.CalcEntryDuration(entryIndexForTime);
			PlayAnimation("Body", ActiveLoopStateHash, ActiveLoopParamHash, num);
			float num2 = Util.Remap(num, minTime, minTime + base.rouletteChestController.bonusTime, 1f, 0f);
			Util.PlaySound(soundCycleEvent, base.gameObject, soundCycleSpeedRtpc, num2 * soundCycleSpeedRtpcScale);
		}

		public override void HandleInteractionServer(Interactor activator)
		{
			base.HandleInteractionServer(activator);
			base.rouletteChestController.EndCycleServer(activator);
		}
	}

	private class Opening : RouletteChestControllerBaseState
	{
		public static float baseDuration;

		public static string soundEntryEvent;

		private static int ActiveToOpeningStateHash = Animator.StringToHash("ActiveToOpening");

		public override void OnEnter()
		{
			base.OnEnter();
			PlayAnimation("Body", ActiveToOpeningStateHash);
			base.rouletteChestController.purchaseInteraction.Networkavailable = false;
			base.rouletteChestController.purchaseInteraction.costType = CostTypeIndex.None;
			Util.PlaySound(soundEntryEvent, base.gameObject);
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge > baseDuration)
			{
				outer.SetNextState(new Opened());
			}
		}
	}

	private class Opened : RouletteChestControllerBaseState
	{
		private static int OpenedStateHash = Animator.StringToHash("Opened");

		public override void OnEnter()
		{
			base.OnEnter();
			PlayAnimation("Body", OpenedStateHash);
			base.rouletteChestController.purchaseInteraction.Networkavailable = false;
			base.rouletteChestController.purchaseInteraction.costType = CostTypeIndex.None;
			base.rouletteChestController.networkIdentity.isPingable = false;
		}
	}

	public int maxEntries = 1;

	public float bonusTime;

	public AnimationCurve bonusTimeDecay;

	public PickupDropTable dropTable;

	public Transform ejectionTransform;

	public Vector3 localEjectionVelocity;

	public Animator modelAnimator;

	public PickupDisplay pickupDisplay;

	private EntityStateMachine stateMachine;

	private PurchaseInteraction purchaseInteraction;

	private NetworkIdentity networkIdentity;

	private static readonly float averageHgReactionTime = 0.20366667f;

	private static readonly float lowestHgReactionTime = 0.15f;

	private static readonly float recognitionWindow = 0.15f;

	private static readonly float minTime = lowestHgReactionTime + recognitionWindow;

	private static readonly float rewindTime = 0.05f;

	private Run.FixedTimeStamp activationTime = Run.FixedTimeStamp.positiveInfinity;

	private Entry[] entries = Array.Empty<Entry>();

	private Xoroshiro128Plus rng;

	private static readonly uint activationTimeDirtyBit = 1u;

	private static readonly uint entriesDirtyBit = 2u;

	private static readonly uint enabledDirtyBit = 4u;

	private static readonly uint allDirtyBitsMask = activationTimeDirtyBit | entriesDirtyBit;

	public int dropCount = 1;

	private int previousEntryIndexClient = -1;

	public UnityEvent onCycleBeginServer;

	public UnityEvent onCycleCompletedServer;

	public UnityEvent onChangedEntryClient;

	private bool isCycling => !activationTime.isPositiveInfinity;

	private float CalcEntryDuration(int i)
	{
		float time = (float)i / (float)maxEntries;
		return bonusTimeDecay.Evaluate(time) * bonusTime + minTime;
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = allDirtyBitsMask;
		}
		writer.WritePackedUInt32(num);
		if ((num & activationTimeDirtyBit) != 0)
		{
			writer.Write(activationTime);
		}
		if ((num & entriesDirtyBit) != 0)
		{
			writer.WritePackedUInt32((uint)entries.Length);
			for (int i = 0; i < entries.Length; i++)
			{
				writer.Write(entries[i].pickupIndex);
			}
		}
		return num != 0;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		uint num = reader.ReadPackedUInt32();
		if ((num & activationTimeDirtyBit) != 0)
		{
			activationTime = reader.ReadFixedTimeStamp();
		}
		if ((num & entriesDirtyBit) != 0)
		{
			Array.Resize(ref entries, (int)reader.ReadPackedUInt32());
			Run.FixedTimeStamp endTime = activationTime;
			for (int i = 0; i < entries.Length; i++)
			{
				ref Entry reference = ref entries[i];
				reference.pickupIndex = reader.ReadPickupIndex();
				reference.endTime = endTime + CalcEntryDuration(i);
				endTime = reference.endTime;
			}
		}
	}

	private void Awake()
	{
		stateMachine = GetComponent<EntityStateMachine>();
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		networkIdentity = GetComponent<NetworkIdentity>();
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		}
	}

	private void OnEnable()
	{
		SetDirtyBit(enabledDirtyBit);
		if ((bool)pickupDisplay)
		{
			pickupDisplay.enabled = true;
		}
	}

	private void OnDisable()
	{
		if ((bool)pickupDisplay)
		{
			pickupDisplay.SetPickupIndex(PickupIndex.none);
			pickupDisplay.enabled = false;
		}
		SetDirtyBit(enabledDirtyBit);
	}

	[Server]
	private void GenerateEntriesServer(Run.FixedTimeStamp startTime)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.RouletteChestController::GenerateEntriesServer(RoR2.Run/FixedTimeStamp)' called on client");
			return;
		}
		Array.Resize(ref entries, maxEntries);
		for (int i = 0; i < entries.Length; i++)
		{
			ref Entry reference = ref entries[i];
			reference.endTime = startTime + CalcEntryDuration(i);
			startTime = reference.endTime;
		}
		PickupIndex pickupIndex = PickupIndex.none;
		for (int j = 0; j < entries.Length; j++)
		{
			ref Entry reference2 = ref entries[j];
			PickupIndex pickupIndex2 = dropTable.GenerateDrop(rng);
			if (pickupIndex2 == pickupIndex)
			{
				pickupIndex2 = dropTable.GenerateDrop(rng);
			}
			reference2.pickupIndex = pickupIndex2;
			pickupIndex = pickupIndex2;
		}
		SetDirtyBit(entriesDirtyBit);
	}

	[Server]
	public void HandleInteractionServer(Interactor activator)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.RouletteChestController::HandleInteractionServer(RoR2.Interactor)' called on client");
		}
		else
		{
			((RouletteChestControllerBaseState)stateMachine.state).HandleInteractionServer(activator);
		}
	}

	[Server]
	private void BeginCycleServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.RouletteChestController::BeginCycleServer()' called on client");
			return;
		}
		activationTime = Run.FixedTimeStamp.now;
		SetDirtyBit(activationTimeDirtyBit);
		GenerateEntriesServer(activationTime);
		onCycleBeginServer?.Invoke();
	}

	[Server]
	private void EndCycleServer([CanBeNull] Interactor activator)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.RouletteChestController::EndCycleServer(RoR2.Interactor)' called on client");
			return;
		}
		float num = 0f;
		if ((bool)activator)
		{
			NetworkUser networkUser = Util.LookUpBodyNetworkUser(activator.gameObject);
			if ((object)networkUser != null)
			{
				num = RttManager.GetConnectionRTT(networkUser.connectionToClient);
			}
		}
		Run.FixedTimeStamp time = Run.FixedTimeStamp.now - num - rewindTime;
		PickupIndex pickupIndexForTime = GetPickupIndexForTime(time);
		EjectPickupServer(pickupIndexForTime);
		activationTime = Run.FixedTimeStamp.positiveInfinity;
		onCycleCompletedServer.Invoke();
	}

	private void FixedUpdate()
	{
		if ((bool)pickupDisplay)
		{
			pickupDisplay.SetPickupIndex(isCycling ? GetPickupIndexForTime(Run.FixedTimeStamp.now) : PickupIndex.none);
		}
		if (NetworkClient.active)
		{
			int entryIndexForTime = GetEntryIndexForTime(Run.FixedTimeStamp.now);
			if (entryIndexForTime != previousEntryIndexClient)
			{
				previousEntryIndexClient = entryIndexForTime;
				onChangedEntryClient.Invoke();
			}
		}
		if (NetworkServer.active && isCycling && entries.Length != 0)
		{
			Run.FixedTimeStamp endTime = entries[entries.Length - 1].endTime;
			if (endTime.hasPassed)
			{
				EndCycleServer(null);
			}
		}
	}

	private int GetEntryIndexForTime(Run.FixedTimeStamp time)
	{
		for (int i = 0; i < entries.Length; i++)
		{
			if (time < entries[i].endTime)
			{
				return i;
			}
		}
		if (entries.Length != 0)
		{
			return entries.Length - 1;
		}
		return -1;
	}

	private PickupIndex GetPickupIndexForTime(Run.FixedTimeStamp time)
	{
		int entryIndexForTime = GetEntryIndexForTime(time);
		if (entryIndexForTime != -1)
		{
			return entries[entryIndexForTime].pickupIndex;
		}
		return PickupIndex.none;
	}

	private void EjectPickupServer(PickupIndex pickupIndex)
	{
		if (!(pickupIndex == PickupIndex.none))
		{
			for (int i = 0; i < dropCount; i++)
			{
				PickupDropletController.CreatePickupDroplet(pickupIndex, ejectionTransform.position, ejectionTransform.rotation * localEjectionVelocity);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
