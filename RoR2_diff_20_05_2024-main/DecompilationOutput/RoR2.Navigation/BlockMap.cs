using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HG;
using Unity.Collections;
using UnityEngine;

namespace RoR2.Navigation;

public class BlockMap<TItem, TItemPositionGetter> where TItemPositionGetter : IPosition3Getter<TItem>
{
	private struct ItemDistanceSqrPair
	{
		public int itemIndex;

		public float distanceSqr;
	}

	private interface ISearchResultHandler
	{
		bool OnEncounterResult(TItem result);
	}

	private struct SingleSearchResultHandler : ISearchResultHandler
	{
		public bool foundResult { get; private set; }

		public TItem result { get; private set; }

		public bool OnEncounterResult(TItem result)
		{
			foundResult = true;
			this.result = result;
			return false;
		}
	}

	private struct ListWriteSearchResultHandler : ISearchResultHandler
	{
		private readonly List<TItem> dest;

		public ListWriteSearchResultHandler(List<TItem> dest)
		{
			this.dest = dest;
		}

		public bool OnEncounterResult(TItem result)
		{
			dest.Add(result);
			return true;
		}
	}

	private struct GridEnumerator
	{
		private readonly Vector3Int startPos;

		private readonly Vector3Int endPos;

		private Vector3Int _current;

		public Vector3Int Current => _current;

		public GridEnumerator(in Vector3Int startCellIndex, in Vector3Int endCellIndex)
		{
			startPos = startCellIndex;
			endPos = endCellIndex;
			_current = startCellIndex;
			ref Vector3Int current = ref _current;
			int x = current.x - 1;
			current.x = x;
		}

		public bool MoveNext()
		{
			if (++_current.x >= endPos.x)
			{
				_current.x = startPos.x;
				if (++_current.z >= endPos.z)
				{
					_current.z = startPos.z;
					if (++_current.y >= endPos.y)
					{
						_current.y = startPos.y;
						return false;
					}
				}
			}
			return true;
		}

		public void Reset()
		{
			_current = startPos;
		}
	}

	private struct GridEnumerable
	{
		private readonly Vector3Int startPos;

		private readonly Vector3Int endPos;

		public GridEnumerable(Vector3Int startPos, Vector3Int endPos)
		{
			this.startPos = startPos;
			this.endPos = endPos;
		}

		public GridEnumerator GetEnumerator()
		{
			return new GridEnumerator(in startPos, in endPos);
		}
	}

	private const bool debugDraw = false;

	private const bool aggressiveDebug = false;

	private Vector3 cellSize;

	private Vector3 invCellSize;

	private Bounds worldBoundingBox;

	private BlockMapCell[] cells = Array.Empty<BlockMapCell>();

	private int cellCount1D;

	private Vector3Int cellCounts;

	private TItem[] itemsPackedByCell = Array.Empty<TItem>();

	private int itemCount;

	private TItemPositionGetter itemPositionGetter;

	public BlockMap()
		: this(new Vector3(15f, 30f, 15f))
	{
	}

	public BlockMap(Vector3 cellSize)
	{
		SetCellSize(cellSize);
	}

	public void Reset()
	{
		worldBoundingBox = default(Bounds);
		ArrayUtils.Clear(itemsPackedByCell, ref itemCount);
		cellCounts = Vector3Int.zero;
		cellCount1D = 0;
	}

	public void SetCellSize(Vector3 newSize)
	{
		if (cellSize != newSize)
		{
			cellSize = newSize;
			invCellSize = new Vector3(1f / cellSize.x, 1f / cellSize.y, 1f / cellSize.z);
			Reset();
		}
	}

	public void Set<T>(T newItems, int newItemsLength, TItemPositionGetter newItemPositionGetter) where T : IList<TItem>
	{
		Reset();
		NativeArray<BlockMapCellIndex> nativeArray = new NativeArray<BlockMapCellIndex>(newItemsLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
		try
		{
			itemPositionGetter = newItemPositionGetter;
			if (newItems.Count > 0)
			{
				worldBoundingBox = new Bounds(itemPositionGetter.GetPosition3(newItems[0]), Vector3.zero);
				for (int i = 1; i < newItems.Count; i++)
				{
					worldBoundingBox.Encapsulate(itemPositionGetter.GetPosition3(newItems[i]));
				}
			}
			worldBoundingBox.min -= Vector3.one;
			worldBoundingBox.max += Vector3.one;
			Vector3 size = worldBoundingBox.size;
			cellCounts = Vector3Int.Max(Vector3Int.CeilToInt(Vector3.Scale(size, invCellSize)), Vector3Int.one);
			cellCount1D = cellCounts.x * cellCounts.y * cellCounts.z;
			Array.Resize(ref cells, cellCount1D);
			Array.Clear(cells, 0, cells.Length);
			_ = worldBoundingBox.min;
			for (int j = 0; j < newItems.Count; j++)
			{
				BlockMapCellIndex blockMapCellIndex2 = (nativeArray[j] = GridPosToCellIndex(WorldPositionToGridPosFloor(itemPositionGetter.GetPosition3(newItems[j]))));
				cells[(int)blockMapCellIndex2].itemCount++;
			}
			int num = 0;
			for (int k = 0; k < cells.Length; k++)
			{
				ref BlockMapCell reference = ref cells[k];
				reference.itemStartIndex = num;
				num += reference.itemCount;
			}
			itemCount = newItems.Count;
			ArrayUtils.EnsureCapacity(ref itemsPackedByCell, itemCount);
			NativeArray<int> nativeArray2 = new NativeArray<int>(cells.Length, Allocator.Temp);
			for (int l = 0; l < itemCount; l++)
			{
				BlockMapCellIndex blockMapCellIndex3 = nativeArray[l];
				ref BlockMapCell reference2 = ref cells[(int)blockMapCellIndex3];
				TItem val = newItems[l];
				int num2 = nativeArray2[(int)blockMapCellIndex3]++;
				itemsPackedByCell[reference2.itemStartIndex + num2] = val;
			}
			nativeArray2.Dispose();
		}
		catch
		{
			Reset();
			throw;
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private Vector3Int WorldPositionToGridPosFloor(in Vector3 worldPosition)
	{
		return LocalPositionToGridPosFloor(worldPosition - worldBoundingBox.min);
	}

	private Vector3Int WorldPositionToGridPosCeil(in Vector3 worldPosition)
	{
		return LocalPositionToGridPosCeil(worldPosition - worldBoundingBox.min);
	}

	private Vector3Int LocalPositionToGridPosFloor(in Vector3 localPosition)
	{
		return Vector3Int.FloorToInt(Vector3.Scale(localPosition, invCellSize));
	}

	private Vector3Int LocalPositionToGridPosCeil(in Vector3 localPosition)
	{
		return Vector3Int.CeilToInt(Vector3.Scale(localPosition, invCellSize));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private BlockMapCellIndex GridPosToCellIndex(in Vector3Int gridPos)
	{
		return (BlockMapCellIndex)((gridPos.y * cellCounts.z + gridPos.z) * cellCounts.x + gridPos.x);
	}

	private Vector3Int CellIndexToGridPos(BlockMapCellIndex cellIndex)
	{
		int num = (int)cellIndex;
		Vector3Int zero = Vector3Int.zero;
		int num2 = cellCounts.x * cellCounts.z;
		zero.y = num / num2;
		num -= zero.y * num2;
		zero.z = num / cellCounts.x;
		num -= zero.z * cellCounts.x;
		zero.x = num;
		return zero;
	}

	private Bounds GridPosToBounds(in Vector3Int gridPos)
	{
		Vector3 vector = gridPos;
		vector.Scale(cellSize);
		vector += worldBoundingBox.min;
		Bounds result = default(Bounds);
		result.SetMinMax(vector, vector + cellSize);
		return result;
	}

	private bool GridPosIsValid(Vector3Int gridPos)
	{
		if (gridPos.x >= 0 && gridPos.y >= 0 && gridPos.z >= 0 && gridPos.x < cellCounts.x && gridPos.y < cellCounts.y)
		{
			return gridPos.z < cellCounts.z;
		}
		return false;
	}

	public void GetItemsInSphere(Vector3 point, float radius, List<TItem> dest)
	{
		Bounds bounds = GetSphereBounds(point, radius);
		int count = dest.Count;
		GetBoundOverlappingCellItems(in bounds, dest);
		float num = radius * radius;
		for (int num2 = dest.Count - 1; num2 >= count; num2--)
		{
			TItem item = dest[num2];
			if ((itemPositionGetter.GetPosition3(item) - point).sqrMagnitude > num)
			{
				dest.RemoveAt(num2);
			}
		}
	}

	public void GetItemsInBounds(in Bounds bounds, List<TItem> dest)
	{
		int count = dest.Count;
		GetBoundOverlappingCellItems(in bounds, dest);
		for (int num = dest.Count - 1; num >= count; num--)
		{
			TItem item = dest[num];
			Vector3 position = itemPositionGetter.GetPosition3(item);
			if (!worldBoundingBox.Contains(position))
			{
				dest.RemoveAt(num);
			}
		}
	}

	private void GetBoundOverlappingCellItems(in Bounds bounds, List<TItem> dest)
	{
		List<BlockMapCellIndex> list = CollectionPool<BlockMapCellIndex, List<BlockMapCellIndex>>.RentCollection();
		GetBoundOverlappingCells(bounds, list);
		foreach (BlockMapCellIndex item in list)
		{
			ref BlockMapCell reference = ref cells[(int)item];
			int i = reference.itemStartIndex;
			for (int num = reference.itemStartIndex + reference.itemCount; i < num; i++)
			{
				dest.Add(itemsPackedByCell[i]);
			}
		}
		list = CollectionPool<BlockMapCellIndex, List<BlockMapCellIndex>>.ReturnCollection(list);
	}

	private void GetBoundOverlappingCells(Bounds bounds, List<BlockMapCellIndex> dest)
	{
		Vector3Int vector3Int = WorldPositionToGridPosFloor(bounds.min);
		Vector3Int vector3Int2 = WorldPositionToGridPosCeil(bounds.max);
		vector3Int.Clamp(Vector3Int.zero, cellCounts);
		vector3Int2.Clamp(Vector3Int.zero, cellCounts);
		Vector3Int gridPos = vector3Int;
		gridPos.y = vector3Int.y;
		while (gridPos.y < vector3Int2.y)
		{
			gridPos.z = vector3Int.z;
			int x;
			while (gridPos.z < vector3Int2.z)
			{
				gridPos.x = vector3Int.x;
				while (gridPos.x < vector3Int2.x)
				{
					dest.Add(GridPosToCellIndex(in gridPos));
					x = gridPos.x + 1;
					gridPos.x = x;
				}
				x = gridPos.z + 1;
				gridPos.z = x;
			}
			x = gridPos.y + 1;
			gridPos.y = x;
		}
	}

	public bool GetNearestItemWhichPassesFilter<TItemFilter>(Vector3 position, float maxDistance, ref TItemFilter filter, out TItem dest) where TItemFilter : IBlockMapSearchFilter<TItem>
	{
		SingleSearchResultHandler searchResultHandler = default(SingleSearchResultHandler);
		GetNearestItemsWhichPassFilter(position, maxDistance, ref filter, ref searchResultHandler);
		dest = (searchResultHandler.foundResult ? searchResultHandler.result : default(TItem));
		return searchResultHandler.foundResult;
	}

	public void GetNearestItemsWhichPassFilter<TItemFilter>(Vector3 position, float maxDistance, ref TItemFilter filter, List<TItem> dest) where TItemFilter : IBlockMapSearchFilter<TItem>
	{
		ListWriteSearchResultHandler searchResultHandler = new ListWriteSearchResultHandler(dest);
		GetNearestItemsWhichPassFilter(position, maxDistance, ref filter, ref searchResultHandler);
	}

	private void GetNearestItemsWhichPassFilter<TItemFilter, TSearchResultHandler>(Vector3 position, float maxDistance, ref TItemFilter filter, ref TSearchResultHandler searchResultHandler) where TItemFilter : IBlockMapSearchFilter<TItem> where TSearchResultHandler : ISearchResultHandler
	{
		maxDistance = Mathf.Max(maxDistance, 0f);
		List<ItemDistanceSqrPair> candidatesInsideRadius = CollectionPool<ItemDistanceSqrPair, List<ItemDistanceSqrPair>>.RentCollection();
		List<ItemDistanceSqrPair> candidatesOutsideRadius = CollectionPool<ItemDistanceSqrPair, List<ItemDistanceSqrPair>>.RentCollection();
		float radiusSqr;
		try
		{
			Vector3Int gridPos = WorldPositionToGridPosFloor(in position);
			Bounds bounds = GridPosToBounds(in gridPos);
			Bounds currentBounds = bounds;
			float additionalRadius2 = cellSize.ComponentMin();
			float radius = DistanceToNearestWall(in position, in currentBounds);
			radiusSqr = radius * radius;
			BlockMapCellIndex cellIndex2 = GridPosToCellIndex(in gridPos);
			BoundsInt visitedCells = new BoundsInt(gridPos, Vector3Int.one);
			int visitedCellCount = 0;
			bool num = GridPosIsValid(gridPos);
			if (num)
			{
				VisitCell(cellIndex2);
			}
			if (!num)
			{
				AddRadius(Mathf.Sqrt(worldBoundingBox.SqrDistance(position)));
			}
			bool flag = true;
			while (flag)
			{
				while (candidatesInsideRadius.Count > 0)
				{
					int num2 = -1;
					float num3 = float.PositiveInfinity;
					for (int i = 0; i < candidatesInsideRadius.Count; i++)
					{
						ItemDistanceSqrPair itemDistanceSqrPair = candidatesInsideRadius[i];
						if (itemDistanceSqrPair.distanceSqr < num3)
						{
							num3 = itemDistanceSqrPair.distanceSqr;
							num2 = i;
						}
					}
					if (num2 != -1)
					{
						ItemDistanceSqrPair itemDistanceSqrPair2 = candidatesInsideRadius[num2];
						candidatesInsideRadius.RemoveAt(num2);
						bool shouldFinish = false;
						if ((filter.CheckItem(itemsPackedByCell[itemDistanceSqrPair2.itemIndex], ref shouldFinish) && !searchResultHandler.OnEncounterResult(itemsPackedByCell[itemDistanceSqrPair2.itemIndex])) || shouldFinish)
						{
							return;
						}
					}
				}
				flag = AddRadius(additionalRadius2);
			}
			bool AddRadius(float additionalRadius)
			{
				radius += additionalRadius;
				bool flag2 = radius >= maxDistance;
				if (flag2)
				{
					radius = maxDistance;
				}
				radiusSqr = radius * radius;
				currentBounds = GetSphereBounds(position, radius);
				for (int num6 = candidatesOutsideRadius.Count - 1; num6 >= 0; num6--)
				{
					ItemDistanceSqrPair item2 = candidatesOutsideRadius[num6];
					if (item2.distanceSqr < radiusSqr)
					{
						candidatesOutsideRadius.RemoveAt(num6);
						candidatesInsideRadius.Add(item2);
					}
				}
				bool flag3 = visitedCellCount >= cellCount1D;
				if (!flag3)
				{
					BoundsInt outer = WorldBoundsToOverlappingGridBoundsClamped(currentBounds);
					BoundsIntDifferenceEnumerable.BoundsIntDifferenceEnumerator enumerator = new BoundsIntDifferenceEnumerable(in outer, in visitedCells).GetEnumerator();
					while (enumerator.MoveNext())
					{
						VisitCell(GridPosToCellIndex(enumerator.Current));
					}
					visitedCells = outer;
				}
				if (candidatesInsideRadius.Count == 0)
				{
					if (flag2)
					{
						return false;
					}
					if (flag3 && candidatesOutsideRadius.Count == 0)
					{
						return false;
					}
				}
				return true;
			}
			void VisitCell(BlockMapCellIndex cellIndex)
			{
				int num4 = visitedCellCount + 1;
				visitedCellCount = num4;
				ref BlockMapCell reference = ref cells[(int)cellIndex];
				int j = reference.itemStartIndex;
				for (int num5 = reference.itemStartIndex + reference.itemCount; j < num5; j++)
				{
					VisitItem(j);
				}
			}
		}
		finally
		{
			candidatesOutsideRadius = CollectionPool<ItemDistanceSqrPair, List<ItemDistanceSqrPair>>.ReturnCollection(candidatesOutsideRadius);
			candidatesInsideRadius = CollectionPool<ItemDistanceSqrPair, List<ItemDistanceSqrPair>>.ReturnCollection(candidatesInsideRadius);
		}
		void VisitItem(int itemIndex)
		{
			Vector3 position2 = itemPositionGetter.GetPosition3(itemsPackedByCell[itemIndex]);
			ItemDistanceSqrPair itemDistanceSqrPair3 = default(ItemDistanceSqrPair);
			itemDistanceSqrPair3.itemIndex = itemIndex;
			itemDistanceSqrPair3.distanceSqr = (position2 - position).sqrMagnitude;
			ItemDistanceSqrPair item = itemDistanceSqrPair3;
			((item.distanceSqr < radiusSqr) ? candidatesInsideRadius : candidatesOutsideRadius).Add(item);
		}
	}

	private float DistanceToNearestWall(in Vector3 position, in Bounds bounds)
	{
		Vector3 vector = position - bounds.min;
		Vector3 vector2 = bounds.max - position;
		float x = vector.x;
		x = ((x < vector.y) ? x : vector.y);
		x = ((x < vector.z) ? x : vector.z);
		x = ((x < vector2.x) ? x : vector2.x);
		x = ((x < vector2.y) ? x : vector2.y);
		return (x < vector2.z) ? x : vector2.z;
	}

	private float DistanceToNearestWall(in Bounds innerBounds, in Bounds outerBounds)
	{
		Vector3 vector = innerBounds.min - outerBounds.min;
		Vector3 vector2 = outerBounds.max - innerBounds.max;
		float x = vector.x;
		x = ((x < vector.y) ? x : vector.y);
		x = ((x < vector.z) ? x : vector.z);
		x = ((x < vector2.x) ? x : vector2.x);
		x = ((x < vector2.y) ? x : vector2.y);
		return (x < vector2.z) ? x : vector2.z;
	}

	private static Bounds GetSphereBounds(Vector3 origin, float radius)
	{
		float num = radius * 2f;
		return new Bounds(origin, new Vector3(num, num, num));
	}

	private BoundsInt WorldBoundsOverlappingToGridBounds(Bounds worldBounds)
	{
		Vector3Int minPosition = WorldPositionToGridPosFloor(worldBounds.min);
		Vector3Int maxPosition = WorldPositionToGridPosCeil(worldBounds.max);
		BoundsInt result = default(BoundsInt);
		result.SetMinMax(minPosition, maxPosition);
		return result;
	}

	private BoundsInt WorldBoundsToOverlappingGridBoundsClamped(Bounds worldBounds)
	{
		Vector3Int minPosition = WorldPositionToGridPosFloor(worldBounds.min);
		Vector3Int maxPosition = WorldPositionToGridPosCeil(worldBounds.max);
		minPosition.Clamp(Vector3Int.zero, cellCounts);
		maxPosition.Clamp(Vector3Int.zero, cellCounts);
		BoundsInt result = default(BoundsInt);
		result.SetMinMax(minPosition, maxPosition);
		return result;
	}

	private GridEnumerable ValidGridPositionsInBounds(Bounds bounds)
	{
		Vector3Int startPos = WorldPositionToGridPosFloor(bounds.min);
		Vector3Int endPos = WorldPositionToGridPosCeil(bounds.max);
		startPos.Clamp(Vector3Int.zero, cellCounts);
		endPos.Clamp(Vector3Int.zero, cellCounts);
		return new GridEnumerable(startPos, endPos);
	}

	private GridEnumerable ValidGridPositionsInBoundsWithExclusion(Bounds bounds, BoundsInt excludedCells)
	{
		Vector3Int startPos = WorldPositionToGridPosFloor(bounds.min);
		Vector3Int endPos = WorldPositionToGridPosCeil(bounds.max);
		startPos.Clamp(Vector3Int.zero, cellCounts - Vector3Int.one);
		endPos.Clamp(Vector3Int.zero, cellCounts - Vector3Int.one);
		return new GridEnumerable(startPos, endPos);
	}
}
