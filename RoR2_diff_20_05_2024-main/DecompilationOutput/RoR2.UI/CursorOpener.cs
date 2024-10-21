using System;
using System.Collections.ObjectModel;
using HG;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class CursorOpener : MonoBehaviour
{
	[SerializeField]
	[Tooltip("If enabled, the cursor will be shown even if the user is on a gamepad.")]
	private bool _forceCursorForGamepad;

	private MPEventSystemLocator eventSystemLocator;

	private static MPEventSystem[] buffer = new MPEventSystem[8];

	private bool _opening;

	protected int linkedEventSystemCount;

	protected MPEventSystem[] linkedEventSystems = Array.Empty<MPEventSystem>();

	protected bool opening
	{
		get
		{
			return _opening;
		}
		set
		{
			if (_opening != value)
			{
				_opening = value;
				RebuildLinks();
			}
		}
	}

	public bool forceCursorForGamePad
	{
		get
		{
			return _forceCursorForGamepad;
		}
		set
		{
			if (_forceCursorForGamepad != value)
			{
				if (linkedEventSystemCount > 0)
				{
					ClearLinkedEventSystems();
				}
				_forceCursorForGamepad = value;
				RebuildLinks();
			}
		}
	}

	private void CacheComponents()
	{
		MPEventSystemLocator component = GetComponent<MPEventSystemLocator>();
		if ((object)component == eventSystemLocator)
		{
			return;
		}
		if ((object)eventSystemLocator != null)
		{
			eventSystemLocator.onEventSystemDiscovered -= OnEventSystemDiscovered;
			eventSystemLocator.onEventSystemLost -= OnEventSystemLost;
			if ((object)eventSystemLocator.eventSystem != null)
			{
				OnEventSystemLost(eventSystemLocator.eventSystem);
			}
		}
		eventSystemLocator = component;
		if ((object)eventSystemLocator != null)
		{
			eventSystemLocator.onEventSystemDiscovered += OnEventSystemDiscovered;
			eventSystemLocator.onEventSystemLost += OnEventSystemLost;
			if ((object)eventSystemLocator.eventSystem != null)
			{
				OnEventSystemDiscovered(eventSystemLocator.eventSystem);
			}
		}
	}

	protected void ClearLinkedEventSystems()
	{
		SetLinkedEventSystems(Array.Empty<MPEventSystem>(), 0);
	}

	protected void SetLinkedEventSystems(MPEventSystem[] newLinkedEventSystems, int newLinkedEventSystemCount)
	{
		for (int num = linkedEventSystemCount - 1; num >= 0; num--)
		{
			ref MPEventSystem reference = ref linkedEventSystems[num];
			MPEventSystem obj = reference;
			int cursorOpenerCount = obj.cursorOpenerCount - 1;
			obj.cursorOpenerCount = cursorOpenerCount;
			if (_forceCursorForGamepad)
			{
				MPEventSystem obj2 = reference;
				cursorOpenerCount = obj2.cursorOpenerForGamepadCount - 1;
				obj2.cursorOpenerForGamepadCount = cursorOpenerCount;
			}
			reference = null;
		}
		ArrayUtils.EnsureCapacity(ref linkedEventSystems, newLinkedEventSystemCount);
		for (int i = 0; i < newLinkedEventSystemCount; i++)
		{
			ref MPEventSystem reference2 = ref linkedEventSystems[i];
			reference2 = newLinkedEventSystems[i];
			MPEventSystem obj3 = reference2;
			int cursorOpenerCount = obj3.cursorOpenerCount + 1;
			obj3.cursorOpenerCount = cursorOpenerCount;
			if (_forceCursorForGamepad)
			{
				MPEventSystem obj4 = reference2;
				cursorOpenerCount = obj4.cursorOpenerForGamepadCount + 1;
				obj4.cursorOpenerForGamepadCount = cursorOpenerCount;
			}
		}
		linkedEventSystemCount = newLinkedEventSystemCount;
	}

	protected void RebuildLinks()
	{
		MPEventSystem eventSystem = eventSystemLocator.eventSystem;
		bool fallBackToMainEventSystem = eventSystemLocator.eventSystemProvider.fallBackToMainEventSystem;
		int count = 0;
		if (opening)
		{
			if (fallBackToMainEventSystem)
			{
				ReadOnlyCollection<MPEventSystem> readOnlyInstancesList = MPEventSystem.readOnlyInstancesList;
				ArrayUtils.EnsureCapacity(ref buffer, readOnlyInstancesList.Count);
				for (int i = 0; i < readOnlyInstancesList.Count; i++)
				{
					buffer[i] = readOnlyInstancesList[i];
				}
				count = readOnlyInstancesList.Count;
			}
			else if ((bool)eventSystemLocator.eventSystem)
			{
				buffer[0] = eventSystem;
				count = 1;
			}
		}
		SetLinkedEventSystems(buffer, count);
		ArrayUtils.Clear(buffer, ref count);
	}

	private void OnEventSystemDiscovered(MPEventSystem discoveredEventSystem)
	{
		if (opening)
		{
			RebuildLinks();
		}
	}

	private void OnEventSystemLost(MPEventSystem lostEventSystem)
	{
		if (opening)
		{
			RebuildLinks();
		}
	}

	protected void Awake()
	{
		CacheComponents();
	}

	protected void OnEnable()
	{
		opening = true;
	}

	protected void OnDisable()
	{
		opening = false;
	}

	[AssetCheck(typeof(CursorOpener))]
	private static void CheckCursorOpener(AssetCheckArgs args)
	{
		if (!((CursorOpener)args.asset).GetComponent<MPEventSystemLocator>())
		{
			args.Log("Missing MPEventSystemLocator.");
		}
	}
}
