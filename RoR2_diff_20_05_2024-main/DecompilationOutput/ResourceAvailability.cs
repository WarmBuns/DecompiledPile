using System;
using JetBrains.Annotations;

public struct ResourceAvailability
{
	public bool available { get; private set; }

	private event Action onAvailable;

	public void MakeAvailable()
	{
		if (!available)
		{
			available = true;
			this.onAvailable?.Invoke();
			this.onAvailable = null;
		}
	}

	public void CallWhenAvailable([NotNull] Action callback)
	{
		if (available)
		{
			callback();
		}
		else
		{
			onAvailable += callback;
		}
	}
}
