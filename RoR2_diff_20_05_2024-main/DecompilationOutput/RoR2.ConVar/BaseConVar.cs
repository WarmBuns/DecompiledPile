using System;
using System.Text;
using HG;
using UnityEngine;

namespace RoR2.ConVar;

public abstract class BaseConVar
{
	public string name;

	public ConVarFlags flags;

	public string defaultValue;

	public string helpText;

	protected BaseConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		this.name = name;
		this.flags = flags;
		this.defaultValue = defaultValue;
		if (helpText == null)
		{
			throw new ArgumentNullException("helpText");
		}
		this.helpText = helpText;
	}

	public void AttemptSetString(string newValue)
	{
		try
		{
			SetString(newValue);
		}
		catch (ConCommandException ex)
		{
			Debug.LogFormat("Could not set value of ConVar \"{0}\" to \"{1}\": {2}", name, newValue, ex.Message);
		}
	}

	public abstract void SetString(string newValue);

	public abstract string GetString();

	protected static void GetEnumValue<T>(string str, ref T dest) where T : struct, Enum
	{
		if (Enum.TryParse<T>(str, out var result))
		{
			dest = result;
			return;
		}
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		stringBuilder.Append("Provided value \"").Append(str).Append("\"")
			.Append(" is not a recognized option. Recognized options: { ");
		bool flag = false;
		string[] names = Enum.GetNames(typeof(T));
		foreach (string value in names)
		{
			if (flag)
			{
				stringBuilder.Append(", ");
			}
			else
			{
				flag = true;
			}
			stringBuilder.Append("\"").Append(value).Append("\"");
		}
		stringBuilder.Append(" }");
		string message = stringBuilder.ToString();
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		throw new ConCommandException(message);
	}

	protected static int ParseIntInvariant(string str)
	{
		if (TextSerialization.TryParseInvariant(str, out int result))
		{
			return result;
		}
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		stringBuilder.Append("Provided value \"").Append(str).Append("\"")
			.Append(" is not a valid number.");
		string message = stringBuilder.ToString();
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		throw new ConCommandException(message);
	}

	protected static float ParseFloatInvariant(string str)
	{
		if (TextSerialization.TryParseInvariant(str, out float result))
		{
			return result;
		}
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		stringBuilder.Append("Provided value \"").Append(str).Append("\"")
			.Append(" is not a valid number.");
		string message = stringBuilder.ToString();
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		throw new ConCommandException(message);
	}

	protected static bool ParseBoolInvariant(string str)
	{
		if (TextSerialization.TryParseInvariant(str, out int result))
		{
			switch (result)
			{
			case 0:
				return false;
			case 1:
				return true;
			}
		}
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		stringBuilder.Append("Provided value \"").Append(str).Append("\"")
			.Append(" was neither \"0\" nor \"1\".");
		string message = stringBuilder.ToString();
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		throw new ConCommandException(message);
	}
}
