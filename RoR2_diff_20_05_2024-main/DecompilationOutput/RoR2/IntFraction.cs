using System;
using System.Text;
using HG;

namespace RoR2;

public readonly struct IntFraction : IEquatable<IntFraction>
{
	public readonly int numerator;

	public readonly int denominator;

	public IntFraction(int numerator, int denominator)
	{
		this.numerator = numerator;
		this.denominator = denominator;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		AppendToStringBuilder(stringBuilder);
		string result = stringBuilder.ToString();
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		return result;
	}

	public void AppendToStringBuilder(StringBuilder stringBuilder)
	{
		stringBuilder.AppendInt(numerator).Append("/").AppendInt(denominator);
	}

	private static bool TryParse(string str, int startIndex, int length, out int result)
	{
		result = 0;
		int i = startIndex;
		int num = startIndex + length;
		if (startIndex < 0 || str.Length < num)
		{
			return false;
		}
		bool flag = false;
		if (i < num && str[i] == '-')
		{
			flag = true;
			i++;
		}
		if (i >= num || !char.IsDigit(str[i]))
		{
			return false;
		}
		for (; i < num; i++)
		{
			int num2 = str[i] - 48;
			if (num2 <= 0 || num2 >= 10)
			{
				break;
			}
			result *= 10;
			result += num2;
		}
		if (flag)
		{
			result = -result;
		}
		return true;
	}

	public static bool TryParse(string str, out IntFraction result)
	{
		result = default(IntFraction);
		int num = 0;
		int num2 = str.IndexOf('/');
		if (num2 == -1)
		{
			return false;
		}
		if (!TryParse(str, 0, num2, out var result2))
		{
			return false;
		}
		num = num2 + 1;
		num2 = str.Length - num;
		if (!TryParse(str, num, str.Length - num2, out var result3))
		{
			return false;
		}
		result = new IntFraction(result2, result3);
		return true;
	}

	public bool Equals(IntFraction other)
	{
		if (numerator == other.numerator)
		{
			return denominator == other.denominator;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is IntFraction other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (numerator * 397) ^ denominator;
	}

	public static explicit operator float(in IntFraction value)
	{
		return (float)value.numerator / (float)value.denominator;
	}
}
