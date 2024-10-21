namespace RoR2.ConVar;

public class StringConVar : BaseConVar
{
	public string value { get; protected set; }

	public StringConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
		: base(name, flags, defaultValue, helpText)
	{
	}

	public override void SetString(string newValue)
	{
		value = newValue;
	}

	public override string GetString()
	{
		return value;
	}
}
