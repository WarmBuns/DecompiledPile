using System;
using System.Collections.Generic;
using System.Text;
using HG;
using JetBrains.Annotations;

public readonly struct KeyValueSplitter
{
	private interface IStringWrapper
	{
		int Length { get; }

		string SubString(int startIndex, int length);
	}

	private struct StringWrapper : IStringWrapper
	{
		private readonly string src;

		public int Length => src.Length;

		public StringWrapper([NotNull] string str)
		{
			src = str;
		}

		public override string ToString()
		{
			return src;
		}

		public string SubString(int startIndex, int length)
		{
			return src.Substring(startIndex, length);
		}
	}

	private struct StringBuilderWrapper : IStringWrapper
	{
		private readonly StringBuilder src;

		public int Length => src.Length;

		public StringBuilderWrapper([NotNull] StringBuilder src)
		{
			this.src = src;
		}

		public override string ToString()
		{
			return src.ToString();
		}

		public string SubString(int startIndex, int length)
		{
			return src.ToString(startIndex, length);
		}
	}

	public readonly string baseKey;

	private readonly int maxKeyLength;

	private readonly int maxValueLength;

	private readonly List<string> currentSubKeys;

	private readonly Action<string, string> keyValueSetter;

	public KeyValueSplitter([NotNull] string baseKey, int maxKeyLength, int maxValueLength, [NotNull] Action<string, string> keyValueSetter)
	{
		this.baseKey = baseKey;
		this.maxKeyLength = maxKeyLength;
		this.maxValueLength = maxValueLength;
		this.keyValueSetter = keyValueSetter;
		currentSubKeys = new List<string>();
	}

	public void SetValue([NotNull] StringBuilder stringBuilder)
	{
		SetValueInternal(new StringBuilderWrapper(stringBuilder));
	}

	public void SetValue([NotNull] string value)
	{
		SetValueInternal(new StringWrapper(value));
	}

	private void SetValueInternal<T>(T value) where T : IStringWrapper
	{
		int length = value.Length;
		List<KeyValuePair<string, string>> list = CollectionPool<KeyValuePair<string, string>, List<KeyValuePair<string, string>>>.RentCollection();
		if (length <= maxValueLength)
		{
			list.Add(new KeyValuePair<string, string>(baseKey, value.ToString()));
		}
		else
		{
			int num = length;
			int num2 = 0;
			StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
			do
			{
				int length2 = Math.Min(num, maxValueLength);
				string key = stringBuilder.Clear().Append(baseKey).Append("[")
					.AppendInt(num2++)
					.Append("]")
					.ToString();
				string value2 = value.SubString(value.Length - num, length2);
				list.Add(new KeyValuePair<string, string>(key, value2));
				num -= maxValueLength;
			}
			while (num > 0);
			HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		}
		for (int i = 0; i < currentSubKeys.Count; i++)
		{
			string text = currentSubKeys[i];
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].Key.Equals(text, StringComparison.Ordinal))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				keyValueSetter(text, null);
			}
		}
		currentSubKeys.Clear();
		for (int k = 0; k < list.Count; k++)
		{
			currentSubKeys.Add(list[k].Key);
		}
		for (int l = 0; l < list.Count; l++)
		{
			KeyValuePair<string, string> keyValuePair = list[l];
			keyValueSetter(keyValuePair.Key, keyValuePair.Value);
		}
		CollectionPool<KeyValuePair<string, string>, List<KeyValuePair<string, string>>>.ReturnCollection(list);
	}
}
