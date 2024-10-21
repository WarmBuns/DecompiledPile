using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RoR2;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct GenericStaticEnumerable<T, TEnumerator> : IEnumerable<T>, IEnumerable where TEnumerator : struct, IEnumerator<T>
{
	private static readonly TEnumerator defaultValue;

	static GenericStaticEnumerable()
	{
		defaultValue = default(TEnumerator);
		defaultValue.Reset();
	}

	public TEnumerator GetEnumerator()
	{
		return defaultValue;
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return defaultValue;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return defaultValue;
	}
}
