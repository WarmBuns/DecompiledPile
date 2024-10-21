using System;
using System.Collections;
using System.Collections.Generic;
using HG;

namespace RoR2;

[Serializable]
public class ItemMask : ICollection<ItemIndex>, IEnumerable<ItemIndex>, IEnumerable
{
	public struct Enumerator : IEnumerator<ItemIndex>, IEnumerator, IDisposable
	{
		private bool[] target;

		public ItemIndex Current { get; private set; }

		object IEnumerator.Current => Current;

		public Enumerator(bool[] array)
		{
			Current = ItemIndex.None;
			target = array;
		}

		public bool MoveNext()
		{
			ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount;
			while (Current < itemCount)
			{
				if (target[(int)Current])
				{
					return true;
				}
				ItemIndex current = Current + 1;
				Current = current;
			}
			return false;
		}

		public void Reset()
		{
			Current = ItemIndex.None;
		}

		public void Dispose()
		{
		}
	}

	private readonly bool[] array;

	public int Count => array.Length;

	public bool IsReadOnly => false;

	public ItemMask()
	{
		array = new bool[ItemCatalog.itemCount];
	}

	public static ItemMask Rent()
	{
		return CollectionPool<ItemIndex, ItemMask>.RentCollection();
	}

	public static void Return(ItemMask itemMask)
	{
		if (itemMask.array.Length == ItemCatalog.itemCount)
		{
			CollectionPool<ItemIndex, ItemMask>.ReturnCollection(itemMask);
		}
	}

	public bool Contains(ItemIndex itemIndex)
	{
		return ArrayUtils.GetSafe(array, (int)itemIndex, false);
	}

	public void Add(ItemIndex itemIndex)
	{
		if (ArrayUtils.IsInBounds(array, (int)itemIndex))
		{
			array[(int)itemIndex] = true;
		}
	}

	public bool Remove(ItemIndex itemIndex)
	{
		if (ArrayUtils.IsInBounds(array, (int)itemIndex))
		{
			ref bool reference = ref array[(int)itemIndex];
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

	public void CopyTo(ItemIndex[] array, int arrayIndex)
	{
		for (int i = 0; i < this.array.Length; i++)
		{
			if (this.array[i])
			{
				array[arrayIndex++] = (ItemIndex)i;
			}
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(array);
	}

	IEnumerator<ItemIndex> IEnumerable<ItemIndex>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
