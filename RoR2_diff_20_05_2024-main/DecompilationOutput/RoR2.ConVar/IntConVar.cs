namespace RoR2.ConVar;

public class IntConVar : BaseConVar
{
	public int value { get; protected set; }

	public IntConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
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
