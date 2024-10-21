using System;

namespace RoR2.Scripts.Utility;

internal class ScopedReset<T> : IDisposable
{
	private T originalValue;

	private Action<T> _setter;

	public ScopedReset(Func<T> getter, Action<T> setter, T tempVal)
	{
		originalValue = getter();
		setter(tempVal);
		_setter = setter;
	}

	public void Dispose()
	{
		_setter(originalValue);
	}
}
