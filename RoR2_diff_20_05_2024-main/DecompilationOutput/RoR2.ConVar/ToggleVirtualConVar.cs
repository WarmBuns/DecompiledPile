using System;
using System.Collections.Generic;

namespace RoR2.ConVar;

public abstract class ToggleVirtualConVar : BaseConVar
{
	private bool _enabled;

	private static readonly List<ToggleVirtualConVar> enabledInstances;

	public bool enabled
	{
		get
		{
			return _enabled;
		}
		protected set
		{
			if (_enabled != value)
			{
				_enabled = value;
				if (_enabled)
				{
					enabledInstances.Add(this);
					OnEnable();
				}
				else
				{
					enabledInstances.Remove(this);
					OnDisable();
				}
			}
		}
	}

	public bool value => enabled;

	static ToggleVirtualConVar()
	{
		enabledInstances = new List<ToggleVirtualConVar>();
		RoR2Application.onShutDown = (Action)Delegate.Combine(RoR2Application.onShutDown, new Action(OnApplicationShutDown));
	}

	public ToggleVirtualConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
		: base(name, flags, defaultValue, helpText)
	{
	}

	protected abstract void OnEnable();

	protected abstract void OnDisable();

	public override void SetString(string newValue)
	{
		enabled = BaseConVar.ParseBoolInvariant(newValue);
	}

	public override string GetString()
	{
		if (!enabled)
		{
			return "0";
		}
		return "1";
	}

	private static void OnApplicationShutDown()
	{
		while (enabledInstances.Count > 0)
		{
			enabledInstances[enabledInstances.Count - 1].enabled = false;
		}
	}
}
