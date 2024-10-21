using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2;

public class CharacterLosTracker : IDisposable
{
	private struct BodyInfo
	{
		public bool hasLos;

		public Run.FixedTimeStamp lastChecked;
	}

	public Vector3 origin;

	public int maxRaycastsPerStep = 4;

	private Dictionary<CharacterBody, BodyInfo> bodyToBodyInfo = new Dictionary<CharacterBody, BodyInfo>();

	private bool _enabled;

	private int currentCheckedBodyIndex;

	public bool enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			if (_enabled != value)
			{
				_enabled = value;
				if (_enabled)
				{
					OnEnable();
				}
				else
				{
					OnDisable();
				}
			}
		}
	}

	public event Action<CharacterBody> onBodyDiscovered;

	public event Action<CharacterBody> onBodyLost;

	private void OnEnable()
	{
		CharacterBody.onBodyAwakeGlobal += OnBodyAwakeGlobal;
		CharacterBody.onBodyDestroyGlobal += OnBodyDestroyGlobal;
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			OnBodyDiscovered(readOnlyInstancesList[i]);
		}
	}

	private void OnDisable()
	{
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			OnBodyLost(readOnlyInstancesList[i]);
		}
		CharacterBody.onBodyDestroyGlobal -= OnBodyDestroyGlobal;
		CharacterBody.onBodyAwakeGlobal -= OnBodyAwakeGlobal;
	}

	private void OnBodyAwakeGlobal(CharacterBody characterBody)
	{
		OnBodyDiscovered(characterBody);
	}

	private void OnBodyDestroyGlobal(CharacterBody characterBody)
	{
		OnBodyLost(characterBody);
	}

	public void Step()
	{
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		if (readOnlyInstancesList.Count == 0)
		{
			return;
		}
		for (int i = 0; i < maxRaycastsPerStep; i++)
		{
			if (++currentCheckedBodyIndex >= readOnlyInstancesList.Count)
			{
				currentCheckedBodyIndex = 0;
			}
			CharacterBody characterBody = readOnlyInstancesList[currentCheckedBodyIndex];
			if ((bool)characterBody.mainHurtBox)
			{
				Vector3 position = characterBody.mainHurtBox.transform.position;
				BodyInfo value = bodyToBodyInfo[characterBody];
				RaycastHit hitInfo;
				bool flag = Physics.Linecast(origin, position, out hitInfo, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
				value.hasLos = !flag;
				bodyToBodyInfo[characterBody] = value;
			}
		}
	}

	public bool CheckBodyHasLos(CharacterBody characterBody)
	{
		if (bodyToBodyInfo.TryGetValue(characterBody, out var value))
		{
			return value.hasLos;
		}
		return false;
	}

	public void Dispose()
	{
		enabled = false;
		bodyToBodyInfo.Clear();
	}

	private void OnBodyDiscovered(CharacterBody characterBody)
	{
		bodyToBodyInfo.Add(characterBody, new BodyInfo
		{
			hasLos = false,
			lastChecked = Run.FixedTimeStamp.now
		});
		try
		{
			this.onBodyDiscovered?.Invoke(characterBody);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void OnBodyLost(CharacterBody characterBody)
	{
		bodyToBodyInfo.Remove(characterBody);
		try
		{
			this.onBodyLost?.Invoke(characterBody);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}
