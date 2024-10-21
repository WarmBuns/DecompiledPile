using UnityEngine;

namespace RoR2.ConVar;

public class PlayerPrefsIntConVar : BaseConVar
{
	private int? defaultValueInt;

	public int value
	{
		get
		{
			if (!defaultValueInt.HasValue && int.TryParse(defaultValue, out var result))
			{
				defaultValueInt = result;
			}
			if (defaultValueInt.HasValue)
			{
				return PlayerPrefs.GetInt(name, defaultValueInt.Value);
			}
			return PlayerPrefs.GetInt(name);
		}
		set
		{
			PlayerPrefs.SetInt(name, value);
			PlayerPrefs.Save();
		}
	}

	public PlayerPrefsIntConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
		: base(name, flags, defaultValue, helpText)
	{
	}

	public override void SetString(string newValue)
	{
		if (TextSerialization.TryParseInvariant(newValue, out int result))
		{
			value = result;
		}
	}

	public override string GetString()
	{
		return TextSerialization.ToStringInvariant(value);
	}
}
