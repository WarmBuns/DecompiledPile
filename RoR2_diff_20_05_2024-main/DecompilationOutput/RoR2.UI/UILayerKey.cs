using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class UILayerKey : MonoBehaviour, IUILayerKeyProvider
{
	public UILayer layer;

	public UnityEvent onBeginRepresentTopLayer;

	public UnityEvent onEndRepresentTopLayer;

	private MPEventSystemLocator eventSystemLocator;

	private MPEventSystem _eventSystem;

	private static readonly Dictionary<MPEventSystem, UILayerKey> topLayerRepresentations = new Dictionary<MPEventSystem, UILayerKey>();

	public bool representsTopLayer { get; private set; }

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		_eventSystem = eventSystemLocator.eventSystem;
	}

	private void Start()
	{
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		if ((bool)_eventSystem)
		{
			RefreshTopLayerForEventSystem(_eventSystem);
		}
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
		if ((bool)_eventSystem)
		{
			RefreshTopLayerForEventSystem(_eventSystem);
		}
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		MPEventSystemManager.availability.CallWhenAvailable(delegate
		{
			ReadOnlyCollection<MPEventSystem> readOnlyInstancesList = MPEventSystem.readOnlyInstancesList;
			for (int i = 0; i < readOnlyInstancesList.Count; i++)
			{
				topLayerRepresentations[readOnlyInstancesList[i]] = null;
			}
		});
		RoR2Application.onLateUpdate += StaticLateUpdate;
	}

	private static void StaticLateUpdate()
	{
		ReadOnlyCollection<MPEventSystem> readOnlyInstancesList = MPEventSystem.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			RefreshTopLayerForEventSystem(readOnlyInstancesList[i]);
		}
	}

	private static void RefreshTopLayerForEventSystem(MPEventSystem eventSystem)
	{
		int num = int.MinValue;
		UILayerKey uILayerKey = null;
		if (!topLayerRepresentations.ContainsKey(eventSystem))
		{
			return;
		}
		UILayerKey uILayerKey2 = topLayerRepresentations[eventSystem];
		List<UILayerKey> instancesList = InstanceTracker.GetInstancesList<UILayerKey>();
		for (int i = 0; i < instancesList.Count; i++)
		{
			UILayerKey uILayerKey3 = instancesList[i];
			if (!(uILayerKey3.eventSystemLocator.eventSystem != eventSystem) && uILayerKey3.layer.priority > num)
			{
				uILayerKey = uILayerKey3;
				num = uILayerKey3.layer.priority;
			}
		}
		if ((object)uILayerKey != uILayerKey2)
		{
			if ((bool)uILayerKey2)
			{
				uILayerKey2.onEndRepresentTopLayer.Invoke();
				uILayerKey2.representsTopLayer = false;
			}
			UILayerKey uILayerKey5 = (topLayerRepresentations[eventSystem] = uILayerKey);
			uILayerKey2 = uILayerKey5;
			if ((bool)uILayerKey2)
			{
				uILayerKey2.representsTopLayer = true;
				uILayerKey2.onBeginRepresentTopLayer.Invoke();
			}
		}
	}

	public UILayerKey GetUILayerKey()
	{
		return this;
	}
}
