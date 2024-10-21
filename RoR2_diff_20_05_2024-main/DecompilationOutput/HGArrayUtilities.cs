using System;
using System.Runtime.CompilerServices;
using HG;
using JetBrains.Annotations;

[Obsolete("HGArrayUtilities is deprecated. Use HG.ArrayUtils instead.")]
public static class HGArrayUtilities
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ArrayInsertNoResize<T>(T[] array, int arraySize, int position, ref T value)
	{
		ArrayUtils.ArrayInsertNoResize(array, arraySize, position, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ArrayInsert<T>(ref T[] array, ref int arraySize, int position, ref T value)
	{
		ArrayUtils.ArrayInsert(ref array, ref arraySize, position, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ArrayInsert<T>(ref T[] array, int position, ref T value)
	{
		ArrayUtils.ArrayInsert(ref array, position, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ArrayAppend<T>(ref T[] array, ref int arraySize, ref T value)
	{
		ArrayUtils.ArrayAppend(ref array, ref arraySize, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ArrayAppend<T>(ref T[] array, ref T value)
	{
		ArrayUtils.ArrayAppend(ref array, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ArrayRemoveAt<T>(ref T[] array, ref int arraySize, int position, int count = 1)
	{
		ArrayUtils.ArrayRemoveAt(array, ref arraySize, position, count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ArrayRemoveAtAndResize<T>(ref T[] array, int position, int count = 1)
	{
		ArrayUtils.ArrayRemoveAtAndResize(ref array, position, count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T GetSafe<T>([NotNull] T[] array, int index)
	{
		return ArrayUtils.GetSafe(array, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T GetSafe<T>([NotNull] T[] array, int index, T defaultValue)
	{
		return ArrayUtils.GetSafe(array, index, in defaultValue);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetAll<T>(T[] array, in T value)
	{
		ArrayUtils.SetAll(array, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void EnsureCapacity<T>(ref T[] array, int capacity)
	{
		ArrayUtils.EnsureCapacity(ref array, capacity);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Swap<T>(T[] array, int a, int b)
	{
		ArrayUtils.Swap(array, a, b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Clear<T>(T[] array, ref int count)
	{
		ArrayUtils.Clear(array, ref count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool SequenceEquals<T>(T[] a, T[] b) where T : IEquatable<T>
	{
		return ArrayUtils.SequenceEquals(a, b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] Clone<T>(T[] src)
	{
		return ArrayUtils.Clone(src);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInBounds<T>(T[] array, int index)
	{
		return ArrayUtils.IsInBounds(array, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInBounds<T>(T[] array, uint index)
	{
		return ArrayUtils.IsInBounds(array, index);
	}
}
