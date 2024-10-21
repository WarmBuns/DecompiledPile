using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2.HudOverlay;

public class OverlayController
{
	public readonly TargetTracker owner;

	public readonly OverlayCreationParams creationParams;

	private readonly List<GameObject> _instancesList = new List<GameObject>();

	private bool _active = true;

	private float _alpha = 1f;

	public IReadOnlyList<GameObject> instancesList => _instancesList;

	public bool active
	{
		get
		{
			return _active;
		}
		set
		{
			if (_active == value)
			{
				return;
			}
			_active = value;
			foreach (GameObject instances in _instancesList)
			{
				instances.SetActive(_active);
			}
		}
	}

	public float alpha
	{
		get
		{
			return _alpha;
		}
		set
		{
			if (_alpha.Equals(value))
			{
				return;
			}
			_alpha = value;
			foreach (GameObject instances in _instancesList)
			{
				PushAlphaToInstance(instances);
			}
		}
	}

	public event Action<OverlayController, GameObject> onInstanceAdded;

	public event Action<OverlayController, GameObject> onInstanceRemove;

	public OverlayController(TargetTracker owner, OverlayCreationParams creationParams)
	{
		this.owner = owner;
		this.creationParams = creationParams;
	}

	public void OnInstanceAdded(GameObject instance)
	{
		_instancesList.Add(instance);
		try
		{
			this.onInstanceAdded?.Invoke(this, instance);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		instance.SetActive(active);
		PushAlphaToInstance(instance);
	}

	public void OnInstanceRemoved(GameObject instance)
	{
		try
		{
			this.onInstanceRemove?.Invoke(this, instance);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
		_instancesList.Remove(instance);
	}

	private void PushAlphaToInstance(GameObject instance)
	{
		CanvasGroup component = instance.GetComponent<CanvasGroup>();
		if ((bool)component)
		{
			component.alpha = alpha;
		}
	}
}
