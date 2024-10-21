using System;
using System.Collections.Generic;
using EntityStates;
using UnityEngine;

namespace RoR2;

public static class BadClientEntityStateMachineFix
{
	private class Watcher
	{
		public LocalUser localUser;

		private TimerQueue.TimerHandle? currentTimerHandle;

		public void OnInstall()
		{
			localUser.onBodyChanged += OnBodyChanged;
		}

		public void OnUninstall()
		{
			localUser.onBodyChanged -= OnBodyChanged;
			if (currentTimerHandle.HasValue)
			{
				RoR2Application.fixedTimeTimers.RemoveTimer(currentTimerHandle.Value);
				currentTimerHandle = null;
			}
		}

		private void OnBodyChanged()
		{
			if (currentTimerHandle.HasValue)
			{
				RoR2Application.fixedTimeTimers.RemoveTimer(currentTimerHandle.Value);
				currentTimerHandle = null;
			}
			if ((bool)localUser.cachedBody)
			{
				currentTimerHandle = RoR2Application.fixedTimeTimers.CreateTimer(1f, OnTimer);
			}
		}

		private void OnTimer()
		{
			currentTimerHandle = null;
			ForceInitializeEntityStateMachines(localUser.cachedBody);
		}

		private static void ForceInitializeEntityStateMachines(CharacterBody characterBody)
		{
			if (!characterBody)
			{
				return;
			}
			EntityStateMachine[] components = characterBody.GetComponents<EntityStateMachine>();
			for (int i = 0; i < components.Length; i++)
			{
				EntityStateMachine entityStateMachine = components[i];
				if (!entityStateMachine.HasPendingState() && entityStateMachine.state is Uninitialized)
				{
					EntityState entityState = EntityStateCatalog.InstantiateState(ref entityStateMachine.mainStateType);
					if (entityState != null)
					{
						Debug.LogFormat("Setting {0} uninitialized state machine [{1}] next state to {2}", characterBody.name, i, entityState.GetType().Name);
						entityStateMachine.SetInterruptState(entityState, InterruptPriority.Any);
					}
				}
			}
		}
	}

	private static readonly Dictionary<LocalUser, Watcher> watchers = new Dictionary<LocalUser, Watcher>();

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		LocalUserManager.onUserSignIn += OnUserSignIn;
		LocalUserManager.onUserSignOut += OnUserSignOut;
	}

	private static void OnUserSignIn(LocalUser localUser)
	{
		Watcher watcher = new Watcher
		{
			localUser = localUser
		};
		watchers[localUser] = watcher;
		watcher.OnInstall();
	}

	private static void OnUserSignOut(LocalUser localUser)
	{
		Watcher watcher = watchers[localUser];
		watcher.OnUninstall();
		watcher.localUser = null;
		watchers.Remove(localUser);
	}
}
