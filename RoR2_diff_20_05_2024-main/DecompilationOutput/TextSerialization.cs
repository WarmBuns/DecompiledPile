using System.Globalization;
using System.Runtime.CompilerServices;

public static class TextSerialization
{
	private static readonly CultureInfo invariantCulture = CultureInfo.InvariantCulture;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ParseIntInvariant(string s)
	{
		return int.Parse(s, NumberStyles.Integer, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ParseUintInvariant(string s)
	{
		return uint.Parse(s, NumberStyles.Integer, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ParseLongInvariant(string s)
	{
		return long.Parse(s, NumberStyles.Integer, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong ParseUlongInvariant(string s)
	{
		return ulong.Parse(s, NumberStyles.Integer, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short ParseShortInvariant(string s)
	{
		return short.Parse(s, NumberStyles.Integer, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort ParseUshortInvariant(string s)
	{
		return ushort.Parse(s, NumberStyles.Integer, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ParseFloatInvariant(string s)
	{
		return float.Parse(s, NumberStyles.Float, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ParseDoubleInvariant(string s)
	{
		return double.Parse(s, NumberStyles.Float, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal ParseDecimalInvariant(string s)
	{
		return decimal.Parse(s, NumberStyles.Float, invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out int result)
	{
		return int.TryParse(s, NumberStyles.Integer, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out uint result)
	{
		return uint.TryParse(s, NumberStyles.Integer, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out long result)
	{
		return long.TryParse(s, NumberStyles.Integer, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out ulong result)
	{
		return ulong.TryParse(s, NumberStyles.Integer, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out short result)
	{
		return short.TryParse(s, NumberStyles.Integer, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out ushort result)
	{
		return ushort.TryParse(s, NumberStyles.Integer, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out float result)
	{
		return float.TryParse(s, NumberStyles.Float, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out double result)
	{
		return double.TryParse(s, NumberStyles.Float, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParseInvariant(string s, out decimal result)
	{
		return decimal.TryParse(s, NumberStyles.Float, invariantCulture, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(int value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(uint value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(long value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(ulong value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(short value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(ushort value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(float value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(double value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringInvariant(decimal value)
	{
		return value.ToString(invariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(int value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(uint value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(long value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(ulong value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(short value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(ushort value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(float value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(double value)
	{
		return value.ToString("N0");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ToStringNumeric(decimal value)
	{
		return value.ToString("N0");
	}
}
