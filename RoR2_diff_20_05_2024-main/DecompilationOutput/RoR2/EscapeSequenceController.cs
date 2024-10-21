using System;
using System.Collections.Generic;
using EntityStates;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class EscapeSequenceController : NetworkBehaviour
{
	[Serializable]
	public struct ScheduledEvent
	{
		public float minSecondsRemaining;

		public float maxSecondsRemaining;

		public UnityEvent onEnter;

		public UnityEvent onExit;

		[NonSerialized]
		public bool inEvent;
	}

	public class EscapeSequenceBaseState : BaseState
	{
		protected EscapeSequenceController escapeSequenceController { get; private set; }

		public override void OnEnter()
		{
			base.OnEnter();
			escapeSequenceController = GetComponent<EscapeSequenceController>();
		}
	}

	public class EscapeSequenceMainState : EscapeSequenceBaseState
	{
		private Run.FixedTimeStamp startTime;

		private Run.FixedTimeStamp endTime;

		public override void OnEnter()
		{
			base.OnEnter();
			if (base.isAuthority)
			{
				startTime = Run.FixedTimeStamp.now;
				endTime = startTime + base.escapeSequenceController.countdownDuration;
			}
			base.escapeSequenceController.onEnterMainEscapeSequence?.Invoke();
		}

		public override void OnExit()
		{
			foreach (HUD readOnlyInstance in HUD.readOnlyInstanceList)
			{
				base.escapeSequenceController.SetHudCountdownEnabled(readOnlyInstance, shouldEnableCountdownPanel: false);
			}
			base.OnExit();
		}

		public override void Update()
		{
			base.Update();
			foreach (HUD readOnlyInstance in HUD.readOnlyInstanceList)
			{
				base.escapeSequenceController.SetHudCountdownEnabled(readOnlyInstance, readOnlyInstance.targetBodyObject);
			}
			base.escapeSequenceController.SetCountdownTime(endTime.timeUntilClamped);
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			base.escapeSequenceController.UpdateScheduledEvents(endTime.timeUntil);
			if (base.isAuthority && endTime.hasPassed && !SceneExitController.isRunning)
			{
				outer.SetNextState(new EscapeSequenceFailureState());
			}
		}

		public override void OnSerialize(NetworkWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(startTime);
			writer.Write(endTime);
		}

		public override void OnDeserialize(NetworkReader reader)
		{
			base.OnDeserialize(reader);
			startTime = reader.ReadFixedTimeStamp();
			endTime = reader.ReadFixedTimeStamp();
		}
	}

	public class EscapeSequenceFailureState : EscapeSequenceBaseState
	{
		public override void OnEnter()
		{
			base.OnEnter();
			if (NetworkServer.active)
			{
				base.escapeSequenceController.onFailEscapeSequenceServer?.Invoke();
			}
		}
	}

	public class EscapeSequenceSuccessState : EscapeSequenceBaseState
	{
		public override void OnEnter()
		{
			base.OnEnter();
			if (NetworkServer.active)
			{
				base.escapeSequenceController.onCompleteEscapeSequenceServer?.Invoke();
			}
		}
	}

	public EntityStateMachine mainStateMachine;

	[Tooltip("How long the player has to escape, in seconds.")]
	public float countdownDuration;

	public UnityEvent onEnterMainEscapeSequence;

	public UnityEvent onCompleteEscapeSequenceServer;

	public UnityEvent onFailEscapeSequenceServer;

	public ScheduledEvent[] scheduledEvents;

	private Dictionary<HUD, GameObject> hudPanels;

	public void BeginEscapeSequence()
	{
		if (Util.HasEffectiveAuthority(base.gameObject))
		{
			mainStateMachine.SetNextState(new EscapeSequenceMainState());
		}
	}

	public void CompleteEscapeSequence()
	{
		if (Util.HasEffectiveAuthority(base.gameObject))
		{
			mainStateMachine.SetNextState(new EscapeSequenceSuccessState());
		}
	}

	private void UpdateScheduledEvents(float secondsRemaining)
	{
		for (int i = 0; i < scheduledEvents.Length; i++)
		{
			ref ScheduledEvent reference = ref scheduledEvents[i];
			bool flag = reference.minSecondsRemaining <= secondsRemaining && secondsRemaining <= reference.maxSecondsRemaining;
			if (flag != reference.inEvent)
			{
				if (flag)
				{
					reference.onEnter?.Invoke();
				}
				else
				{
					reference.onExit?.Invoke();
				}
				reference.inEvent = flag;
			}
		}
	}

	private void SetHudCountdownEnabled(HUD hud, bool shouldEnableCountdownPanel)
	{
		shouldEnableCountdownPanel &= base.enabled;
		hudPanels.TryGetValue(hud, out var value);
		if ((bool)value == shouldEnableCountdownPanel)
		{
			return;
		}
		if (shouldEnableCountdownPanel)
		{
			RectTransform rectTransform = hud.GetComponent<ChildLocator>().FindChild("TopCenterCluster") as RectTransform;
			if ((bool)rectTransform)
			{
				GameObject value2 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/HudModules/HudCountdownPanel"), rectTransform);
				hudPanels[hud] = value2;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(value);
			hudPanels.Remove(hud);
		}
	}

	private void SetCountdownTime(double secondsRemaining)
	{
		foreach (KeyValuePair<HUD, GameObject> hudPanel in hudPanels)
		{
			hudPanel.Value.GetComponent<TimerText>().seconds = secondsRemaining;
		}
		AkSoundEngine.SetRTPCValue("EscapeTimer", Util.Remap((float)secondsRemaining, 0f, countdownDuration, 0f, 100f));
	}

	private void OnEnable()
	{
		hudPanels = new Dictionary<HUD, GameObject>();
	}

	private void OnDisable()
	{
		foreach (HUD readOnlyInstance in HUD.readOnlyInstanceList)
		{
			SetHudCountdownEnabled(readOnlyInstance, shouldEnableCountdownPanel: false);
		}
		hudPanels = null;
	}

	public void DestroyAllBodies()
	{
		List<CharacterBody> list = new List<CharacterBody>(CharacterBody.readOnlyInstancesList);
		for (int i = 0; i < list.Count; i++)
		{
			CharacterBody characterBody = list[i];
			if ((bool)characterBody)
			{
				UnityEngine.Object.Destroy(characterBody.gameObject);
			}
		}
	}

	public void KillAllCharacters()
	{
		List<CharacterMaster> list = new List<CharacterMaster>(CharacterMaster.readOnlyInstancesList);
		for (int i = 0; i < list.Count; i++)
		{
			CharacterMaster characterMaster = list[i];
			if ((bool)characterMaster)
			{
				characterMaster.TrueKill(null, null, DamageType.Silent | DamageType.VoidDeath);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
