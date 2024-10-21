using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public static class StringBuilderExtensions
{
	private const uint maxDigitsInUint = 10u;

	private const uint maxDigitsInUlong = 20u;

	private static readonly char[] hexLookup = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'A', 'B', 'C', 'D', 'E', 'F'
	};

	public static StringBuilder AppendInt(this StringBuilder stringBuilder, int value, uint minDigits = 1u, uint maxDigits = uint.MaxValue)
	{
		return AppendSignedInternal(stringBuilder, value, 10u, minDigits, maxDigits);
	}

	public static StringBuilder AppendUint(this StringBuilder stringBuilder, uint value, uint minDigits = 1u, uint maxDigits = uint.MaxValue)
	{
		return AppendUnsignedInternal(stringBuilder, value, 10u, minDigits, maxDigits);
	}

	public static StringBuilder AppendLong(this StringBuilder stringBuilder, long value, uint minDigits = 1u, uint maxDigits = uint.MaxValue)
	{
		return AppendSignedInternal(stringBuilder, value, 20u, minDigits, maxDigits);
	}

	public static StringBuilder AppendUlong(this StringBuilder stringBuilder, ulong value, uint minDigits = 1u, uint maxDigits = uint.MaxValue)
	{
		return AppendUnsignedInternal(stringBuilder, value, 20u, minDigits, maxDigits);
	}

	private static StringBuilder AppendUnsignedInternal(StringBuilder stringBuilder, ulong value, uint maxDigitsForType, uint minDigits = 1u, uint maxDigits = uint.MaxValue)
	{
		if (maxDigits < minDigits)
		{
			throw new ArgumentException("minDigits cannot be greater than maxDigits.");
		}
		uint num = 0u;
		uint num2 = ((maxDigitsForType < maxDigits) ? maxDigitsForType : maxDigits);
		ulong num3 = 1uL;
		while (num3 <= value && num < num2)
		{
			num3 *= 10;
			num++;
		}
		uint num4 = ((minDigits < num) ? num : minDigits);
		if (num4 != 0)
		{
			int length = stringBuilder.Length;
			stringBuilder.Append('0', (int)num4);
			ulong num5 = 0uL;
			ulong num6 = (uint)(length + (int)num4 - 1);
			ulong num7 = value;
			while (num5 < num)
			{
				ulong num8 = num7 / 10;
				byte b = (byte)(num7 - num8 * 10);
				char value2 = (char)(48 + b);
				stringBuilder[(int)num6] = value2;
				num7 = num8;
				num5++;
				num6--;
			}
		}
		return stringBuilder;
	}

	private static StringBuilder AppendSignedInternal(StringBuilder stringBuilder, long value, uint maxDigitsForType, uint minDigits = 1u, uint maxDigits = uint.MaxValue)
	{
		if (maxDigits < minDigits)
		{
			throw new ArgumentException("minDigits cannot be greater than maxDigits.");
		}
		if (value < 0)
		{
			stringBuilder.Append('-');
			value = -value;
		}
		return AppendUnsignedInternal(stringBuilder, (uint)value, maxDigitsForType, minDigits, maxDigits);
	}

	private static void GetByteHexChars(byte byteValue, out char char1, out char char2)
	{
		char1 = hexLookup[(byteValue >> 4) & 0xF];
		char2 = hexLookup[byteValue & 0xF];
	}

	public static StringBuilder AppendByteHexValue(this StringBuilder stringBuilder, byte value)
	{
		GetByteHexChars(value, out var @char, out var char2);
		return stringBuilder.Append(@char).Append(char2);
	}

	public static StringBuilder AppendColor32RGBHexValues(this StringBuilder stringBuilder, Color32 color)
	{
		int num = stringBuilder.Length + 6;
		if (stringBuilder.Capacity < num)
		{
			stringBuilder.Capacity = num;
		}
		GetByteHexChars(color.r, out var @char, out var char2);
		GetByteHexChars(color.g, out var char3, out var char4);
		GetByteHexChars(color.b, out var char5, out var char6);
		return stringBuilder.Append(@char).Append(char2).Append(char3)
			.Append(char4)
			.Append(char5)
			.Append(char6);
	}

	public static StringBuilder JoinEnumerator<TElement, TEnumerator>(this StringBuilder stringBuilder, [NotNull] string separator, TEnumerator enumerator) where TEnumerator : IEnumerator<TElement>
	{
		if (enumerator.MoveNext())
		{
			stringBuilder.Append(enumerator.Current);
			while (enumerator.MoveNext())
			{
				stringBuilder.Append(separator);
				stringBuilder.Append(enumerator.Current);
			}
		}
		return stringBuilder;
	}

	public static string Take(this StringBuilder stringBuilder)
	{
		string result = stringBuilder.ToString();
		stringBuilder.Clear();
		return result;
	}
}
