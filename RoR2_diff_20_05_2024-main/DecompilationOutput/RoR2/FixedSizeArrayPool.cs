using System;
using System.Runtime.CompilerServices;
using HG;
using JetBrains.Annotations;

namespace RoR2;

public class FixedSizeArrayPool<T>
{
	public enum ClearType
	{
		RESET,
		CLEAR,
		NONE
	}

	private int _lengthOfArrays;

	[NotNull]
	private T[][] pooledArrays;

	private int count;

	public int lengthOfArrays
	{
		get
		{
			return _lengthOfArrays;
		}
		set
		{
			if (_lengthOfArrays != value)
			{
				ArrayUtils.Clear(pooledArrays, ref count);
				_lengthOfArrays = value;
			}
		}
	}

	public FixedSizeArrayPool(int lengthOfArrays)
	{
		_lengthOfArrays = lengthOfArrays;
		pooledArrays = Array.Empty<T[]>();
	}

	[NotNull]
	public T[] Request()
	{
		if (count <= 0)
		{
			return new T[_lengthOfArrays];
		}
		T[] result = pooledArrays[--count];
		pooledArrays[count] = null;
		return result;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[NotNull]
	private static ArgumentException CreateArraySizeMismatchException(int incomingArrayLength, int arrayPoolSizeRequirement)
	{
		return new ArgumentException($"Array of length {incomingArrayLength} may not be returned to pool for arrays of length {arrayPoolSizeRequirement}", "array");
	}

	public void Return([NotNull] T[] array, ClearType clearType = ClearType.RESET)
	{
		if (array.Length != _lengthOfArrays)
		{
			throw CreateArraySizeMismatchException(array.Length, _lengthOfArrays);
		}
		switch (clearType)
		{
		case ClearType.RESET:
			ArrayUtils.SetAll(array, default(T));
			break;
		case ClearType.CLEAR:
			Array.Clear(array, 0, array.Length);
			break;
		}
		ArrayUtils.ArrayAppend(ref pooledArrays, ref count, in array);
	}
}
