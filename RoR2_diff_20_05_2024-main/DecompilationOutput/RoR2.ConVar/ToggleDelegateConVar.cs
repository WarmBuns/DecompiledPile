using System;

namespace RoR2.ConVar;

public sealed class ToggleDelegateConVar : ToggleVirtualConVar
{
	private readonly Action onEnable;

	private readonly Action onDisable;

	public ToggleDelegateConVar(string name, ConVarFlags flags, string defaultValue, string helpText, Action onEnable, Action onDisable)
		: base(name, flags, defaultValue, helpText)
	{
		this.onEnable = onEnable;
		this.onDisable = onDisable;
	}

	protected override void OnEnable()
	{
		onEnable?.Invoke();
	}

	protected override void OnDisable()
	{
		onDisable?.Invoke();
	}
}
