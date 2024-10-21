using System;
using System.Collections.Generic;
using System.Xml.Linq;
using JetBrains.Annotations;
using UnityEngine;

public static class HGXml
{
	public delegate void Serializer<T>(XElement element, T contents);

	public delegate bool Deserializer<T>(XElement element, ref T contents);

	public class SerializationRules<T>
	{
		public Serializer<T> serializer;

		public Deserializer<T> deserializer;

		public static SerializationRules<T> defaultRules;

		static SerializationRules()
		{
			if (typeof(T).IsEnum)
			{
				RegisterEnum();
			}
		}

		private static void RegisterEnum()
		{
			Type typeFromHandle = typeof(T);
			Dictionary<string, T> nameToValue = new Dictionary<string, T>();
			string[] names = Enum.GetNames(typeFromHandle);
			for (int i = 0; i < names.Length; i++)
			{
				string text = names[i];
				Enum.Parse(typeFromHandle, names[i]);
				nameToValue[text] = (T)Enum.Parse(typeFromHandle, text);
			}
			Dictionary<T, string> valueToName = new Dictionary<T, string>();
			Array values = Enum.GetValues(typeFromHandle);
			for (int j = 0; j < values.Length; j++)
			{
				object value = values.GetValue(j);
				valueToName[(T)value] = Enum.GetName(typeFromHandle, value);
			}
			defaultRules = new SerializationRules<T>
			{
				serializer = Serializer,
				deserializer = Deserializer
			};
			bool Deserializer(XElement element, ref T contents)
			{
				if (nameToValue.TryGetValue(element.Value, out var value3))
				{
					contents = value3;
					return true;
				}
				try
				{
					contents = (T)Enum.Parse(typeof(T), element.Value);
					return true;
				}
				catch (Exception)
				{
				}
				return false;
			}
			void Serializer(XElement element, T contents)
			{
				if (valueToName.TryGetValue(contents, out var value2))
				{
					element.Value = value2;
				}
				else
				{
					element.Value = contents.ToString();
				}
			}
		}
	}

	public static void Register<T>(Serializer<T> serializer, Deserializer<T> deserializer)
	{
		SerializationRules<T>.defaultRules = new SerializationRules<T>
		{
			serializer = serializer,
			deserializer = deserializer
		};
	}

	[NotNull]
	public static XElement ToXml<T>(string name, T value)
	{
		return ToXml(name, value, SerializationRules<T>.defaultRules);
	}

	[NotNull]
	public static XElement ToXml<T>(string name, T value, SerializationRules<T> rules)
	{
		XElement xElement = new XElement(name);
		rules.serializer(xElement, value);
		return xElement;
	}

	public static bool FromXml<T>([NotNull] XElement element, ref T value)
	{
		return FromXml(element, ref value, SerializationRules<T>.defaultRules);
	}

	public static bool FromXml<T>([NotNull] XElement element, ref T value, SerializationRules<T> rules)
	{
		if (rules == null)
		{
			Debug.LogFormat("Serialization rules not defined for type <{0}>", typeof(T).Name);
			return false;
		}
		return rules.deserializer(element, ref value);
	}

	public static bool FromXml<T>([NotNull] XElement element, [NotNull] Action<T> setter)
	{
		return FromXml(element, setter, SerializationRules<T>.defaultRules);
	}

	public static bool FromXml<T>([NotNull] XElement element, [NotNull] Action<T> setter, [NotNull] SerializationRules<T> rules)
	{
		T value = default(T);
		if (FromXml(element, ref value, rules))
		{
			setter(value);
			return true;
		}
		return false;
	}

	static HGXml()
	{
		Register(delegate(XElement element, int contents)
		{
			element.Value = TextSerialization.ToStringInvariant(contents);
		}, delegate(XElement element, ref int contents)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out int result))
			{
				contents = result;
				return true;
			}
			return false;
		});
		Register(delegate(XElement element, uint contents)
		{
			element.Value = TextSerialization.ToStringInvariant(contents);
		}, delegate(XElement element, ref uint contents)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out uint result2))
			{
				contents = result2;
				return true;
			}
			return false;
		});
		Register(delegate(XElement element, ulong contents)
		{
			element.Value = TextSerialization.ToStringInvariant(contents);
		}, delegate(XElement element, ref ulong contents)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out ulong result3))
			{
				contents = result3;
				return true;
			}
			return false;
		});
		Register(delegate(XElement element, bool contents)
		{
			element.Value = (contents ? "1" : "0");
		}, delegate(XElement element, ref bool contents)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out int result4))
			{
				contents = result4 != 0;
				return true;
			}
			return false;
		});
		Register(delegate(XElement element, float contents)
		{
			element.Value = TextSerialization.ToStringInvariant(contents);
		}, delegate(XElement element, ref float contents)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out float result5))
			{
				contents = result5;
				return true;
			}
			return false;
		});
		Register(delegate(XElement element, double contents)
		{
			element.Value = TextSerialization.ToStringInvariant(contents);
		}, delegate(XElement element, ref double contents)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out double result6))
			{
				contents = result6;
				return true;
			}
			return false;
		});
		Register(delegate(XElement element, string contents)
		{
			element.Value = contents;
		}, delegate(XElement element, ref string contents)
		{
			contents = element.Value;
			return true;
		});
		Register(delegate(XElement element, Guid contents)
		{
			element.Value = contents.ToString();
		}, delegate(XElement element, ref Guid contents)
		{
			if (Guid.TryParse(element.Value, out var result7))
			{
				contents = result7;
				return true;
			}
			return false;
		});
		Register(delegate(XElement element, DateTime contents)
		{
			element.Value = TextSerialization.ToStringInvariant(contents.ToBinary());
		}, delegate(XElement element, ref DateTime contents)
		{
			if (TextSerialization.TryParseInvariant(element.Value, out long result8))
			{
				try
				{
					contents = DateTime.FromBinary(result8);
					return true;
				}
				catch (ArgumentException)
				{
				}
			}
			return false;
		});
	}

	public static void Deserialize<T>(this XElement element, ref T dest)
	{
		FromXml(element, ref dest);
	}

	public static void Deserialize<T>(this XElement element, ref T dest, SerializationRules<T> rules)
	{
		FromXml(element, ref dest, rules);
	}

	public static void UsedOnlyForAOTCodeGeneration()
	{
		throw new InvalidOperationException("This method is used for AOT code generation only. Do not call it at runtime.");
	}
}
