using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2.UI;

public class MPEventSystemProvider : MonoBehaviour
{
	[FormerlySerializedAs("eventSystem")]
	[SerializeField]
	private MPEventSystem _eventSystem;

	public bool fallBackToMainEventSystem = true;

	public bool fallBackToPlayer2EventSystem;

	public bool fallBackToPlayer3EventSystem;

	public bool fallBackToPlayer4EventSystem;

	private MPEventSystem _resolvedEventSystem;

	private List<MPEventSystemLocator> listeners = new List<MPEventSystemLocator>();

	public MPEventSystem eventSystem
	{
		get
		{
			return _eventSystem;
		}
		set
		{
			if ((object)_eventSystem != value)
			{
				_eventSystem = value;
				ResolveEventSystem();
			}
		}
	}

	public MPEventSystem resolvedEventSystem
	{
		get
		{
			return _resolvedEventSystem;
		}
		private set
		{
			if ((object)_resolvedEventSystem != value)
			{
				_resolvedEventSystem = value;
				for (int num = listeners.Count - 1; num >= 0; num--)
				{
					listeners[num].eventSystem = _resolvedEventSystem;
				}
			}
		}
	}

	private void Awake()
	{
		ResolveEventSystem();
	}

	private void OnEnable()
	{
		ResolveEventSystem();
	}

	private void OnDisable()
	{
		resolvedEventSystem = null;
	}

	private void OnDestroy()
	{
		for (int num = listeners.Count - 1; num >= 0; num--)
		{
			listeners[num].OnProviderDestroyed(this);
		}
	}

	private void Update()
	{
		ResolveEventSystem();
	}

	private void ResolveEventSystem()
	{
		if ((bool)eventSystem)
		{
			resolvedEventSystem = eventSystem;
		}
		else if (fallBackToMainEventSystem)
		{
			resolvedEventSystem = MPEventSystemManager.primaryEventSystem;
		}
		else if (fallBackToPlayer2EventSystem)
		{
			resolvedEventSystem = MPEventSystem.FindByPlayer(LocalUserManager.FindLocalUserByPlayerName("Player2")?.inputPlayer ?? null);
		}
		else if (fallBackToPlayer3EventSystem)
		{
			resolvedEventSystem = MPEventSystem.FindByPlayer(LocalUserManager.FindLocalUserByPlayerName("Player3")?.inputPlayer ?? null);
		}
		else if (fallBackToPlayer4EventSystem)
		{
			resolvedEventSystem = MPEventSystem.FindByPlayer(LocalUserManager.FindLocalUserByPlayerName("Player4")?.inputPlayer ?? null);
		}
	}

	internal void AddListener(MPEventSystemLocator listener)
	{
		listeners.Add(listener);
		listener.eventSystem = resolvedEventSystem;
	}

	internal void RemoveListener(MPEventSystemLocator listener)
	{
		listener.eventSystem = null;
		listeners.Remove(listener);
	}
}
