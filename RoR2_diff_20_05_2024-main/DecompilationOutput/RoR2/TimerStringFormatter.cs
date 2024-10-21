using System;
using System.Text;
using HG;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/Timer String Formatter")]
public class TimerStringFormatter : ScriptableObject
{
	[Serializable]
	public struct Format
	{
		[Serializable]
		public struct Unit
		{
			[CanBeNull]
			public string name;

			public double conversionRate;

			public uint minDigits;

			public uint maxDigits;

			[CanBeNull]
			public string prefix;

			[CanBeNull]
			public string suffix;
		}

		[CanBeNull]
		public Unit[] units;

		public string prefix;

		public string suffix;

		public static Format Clone(in Format src)
		{
			Format result = default(Format);
			result.prefix = src.prefix;
			result.suffix = src.suffix;
			result.units = ((src.units != null) ? ArrayUtils.Clone(src.units) : null);
			return result;
		}
	}

	public Format format = defaultFormat;

	private const double hoursPerDay = 24.0;

	private const double minutesPerDay = 1440.0;

	private const double secondsPerDay = 86400.0;

	private const double minutesPerHour = 60.0;

	private const double secondsPerMinute = 60.0;

	private const double secondsPerSecond = 1.0;

	private const double secondsPerCentisecond = 0.01;

	private const double secondsPerHour = 3600.0;

	private static readonly Format defaultFormat = new Format
	{
		prefix = "<mspace=0.5em>",
		suffix = "</mspace>",
		units = new Format.Unit[5]
		{
			new Format.Unit
			{
				name = "days",
				conversionRate = 86400.0,
				maxDigits = 2147483647u,
				minDigits = 2u,
				prefix = string.Empty,
				suffix = string.Empty
			},
			new Format.Unit
			{
				name = "hours",
				conversionRate = 3600.0,
				maxDigits = 2u,
				minDigits = 2u,
				prefix = ":",
				suffix = string.Empty
			},
			new Format.Unit
			{
				name = "minutes",
				conversionRate = 60.0,
				maxDigits = 2u,
				minDigits = 2u,
				prefix = ":",
				suffix = string.Empty
			},
			new Format.Unit
			{
				name = "seconds",
				conversionRate = 1.0,
				maxDigits = 2u,
				minDigits = 2u,
				prefix = ":",
				suffix = string.Empty
			},
			new Format.Unit
			{
				name = "centiseconds",
				conversionRate = 0.01,
				maxDigits = 2u,
				minDigits = 2u,
				prefix = "<voffset=0.4em><size=40%><mspace=0.5em>.",
				suffix = "</size></voffset></mspace>"
			}
		}
	};

	public void AppendToStringBuilder(double seconds, [NotNull] StringBuilder dest)
	{
		double num = seconds;
		if (!string.IsNullOrEmpty(format.prefix))
		{
			dest.Append(format.prefix);
		}
		if (num < 0.0)
		{
			num = 0.0 - num;
			dest.Append("-");
		}
		Format.Unit[] units = format.units;
		for (int i = 0; i < units.Length; i++)
		{
			Format.Unit unit = units[i];
			double num2 = Math.Floor(num / unit.conversionRate);
			num -= num2 * unit.conversionRate;
			int num3 = (int)num2;
			uint minDigits = unit.minDigits;
			uint num4 = unit.maxDigits;
			if (num4 < minDigits)
			{
				num4 = minDigits;
			}
			if (num4 != 0 && (minDigits != 0 || num3 >= 10))
			{
				if (!string.IsNullOrEmpty(unit.prefix))
				{
					dest.Append(unit.prefix);
				}
				dest.AppendInt(num3, minDigits, num4);
				if (!string.IsNullOrEmpty(unit.suffix))
				{
					dest.Append(unit.suffix);
				}
			}
		}
		if (!string.IsNullOrEmpty(format.suffix))
		{
			dest.Append(format.suffix);
		}
	}

	private void Reset()
	{
		format = Format.Clone(in defaultFormat);
	}
}
