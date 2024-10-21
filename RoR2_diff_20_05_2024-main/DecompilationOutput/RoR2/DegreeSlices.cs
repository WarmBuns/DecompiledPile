using System;
using System.Collections;
using System.Collections.Generic;

namespace RoR2;

public struct DegreeSlices : IEnumerable<float>, IEnumerable
{
	public struct Enumerator : IEnumerator<float>, IEnumerator, IDisposable
	{
		public readonly float sliceSize;

		public readonly float offset;

		public int i;

		public int iEnd;

		public float Current => (float)i * sliceSize + offset;

		object IEnumerator.Current => Current;

		public Enumerator(int sliceCount, float sliceOffset)
		{
			sliceSize = 360f / (float)sliceCount;
			offset = sliceOffset * sliceSize;
			i = -1;
			iEnd = sliceCount;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			i++;
			return i < iEnd;
		}

		public void Reset()
		{
			i = -1;
		}
	}

	public readonly int sliceCount;

	public readonly float sliceOffset;

	public DegreeSlices(int sliceCount, float sliceOffset)
	{
		this.sliceCount = sliceCount;
		this.sliceOffset = sliceOffset;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(sliceCount, sliceOffset);
	}

	IEnumerator<float> IEnumerable<float>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
