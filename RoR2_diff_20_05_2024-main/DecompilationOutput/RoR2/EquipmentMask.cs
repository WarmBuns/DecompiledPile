using System;
using System.Collections;
using System.Collections.Generic;
using HG;

namespace RoR2;

[Serializable]
public class EquipmentMask : ICollection<EquipmentIndex>, IEnumerable<EquipmentIndex>, IEnumerable
{
	public struct Enumerator : IEnumerator<EquipmentIndex>, IEnumerator, IDisposable
	{
		private bool[] target;

		public EquipmentIndex Current { get; private set; }

		object IEnumerator.Current => Current;

		public Enumerator(bool[] array)
		{
			Current = EquipmentIndex.None;
			target = array;
		}

		public bool MoveNext()
		{
			EquipmentIndex equipmentCount = (EquipmentIndex)EquipmentCatalog.equipmentCount;
			while (Current < equipmentCount)
			{
				if (target[(int)Current])
				{
					return true;
				}
				EquipmentIndex current = Current + 1;
				Current = current;
			}
			return false;
		}

		public void Reset()
		{
			Current = EquipmentIndex.None;
		}

		public void Dispose()
		{
		}
	}

	private readonly bool[] array;

	public int Count => array.Length;

	public bool IsReadOnly => false;

	public EquipmentMask()
	{
		array = new bool[EquipmentCatalog.equipmentCount];
	}

	public static EquipmentMask Rent()
	{
		return CollectionPool<EquipmentIndex, EquipmentMask>.RentCollection();
	}

	public static void Return(EquipmentMask equipmentMask)
	{
		if (equipmentMask.array.Length == EquipmentCatalog.equipmentCount)
		{
			CollectionPool<EquipmentIndex, EquipmentMask>.ReturnCollection(equipmentMask);
		}
	}

	public bool Contains(EquipmentIndex equipmentIndex)
	{
		return ArrayUtils.GetSafe(array, (int)equipmentIndex, false);
	}

	public void Add(EquipmentIndex equipmentIndex)
	{
		if (ArrayUtils.IsInBounds(array, (int)equipmentIndex))
		{
			array[(int)equipmentIndex] = true;
		}
	}

	public bool Remove(EquipmentIndex equipmentIndex)
	{
		if (ArrayUtils.IsInBounds(array, (int)equipmentIndex))
		{
			ref bool reference = ref array[(int)equipmentIndex];
			bool result = reference;
			reference = false;
			return result;
		}
		return false;
	}

	public void Clear()
	{
		ArrayUtils.SetAll(array, false);
	}

	public void CopyTo(EquipmentIndex[] array, int arrayIndex)
	{
		for (int i = 0; i < this.array.Length; i++)
		{
			if (this.array[i])
			{
				array[arrayIndex++] = (EquipmentIndex)i;
			}
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(array);
	}

	IEnumerator<EquipmentIndex> IEnumerable<EquipmentIndex>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
