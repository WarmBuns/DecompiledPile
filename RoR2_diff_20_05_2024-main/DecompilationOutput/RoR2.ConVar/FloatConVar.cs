namespace RoR2.ConVar;

public class FloatConVar : BaseConVar
{
	public float value { get; protected set; }

	public FloatConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
		: base(name, flags, defaultValue, helpText)
	{
	}

	public override void SetString(string newValue)
	{
		if (TextSerialization.TryParseInvariant(newValue, out float result) && !float.IsNaN(result) && !float.IsInfinity(result))
		{
			value = result;
		}
	}

	public override string GetString()
	{
		return TextSerialization.ToStringInvariant(value);
	}
}
