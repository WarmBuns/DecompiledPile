namespace RoR2.ConVar;

public class BoolConVar : BaseConVar
{
	public bool value { get; protected set; }

	public BoolConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
		: base(name, flags, defaultValue, helpText)
	{
	}

	public void SetBool(bool newValue)
	{
		value = newValue;
	}

	public override void SetString(string newValue)
	{
		if (TextSerialization.TryParseInvariant(newValue, out int result))
		{
			value = result != 0;
		}
	}

	public override string GetString()
	{
		if (!value)
		{
			return "0";
		}
		return "1";
	}
}
