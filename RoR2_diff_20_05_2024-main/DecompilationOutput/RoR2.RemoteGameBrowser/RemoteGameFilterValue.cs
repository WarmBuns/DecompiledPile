namespace RoR2.RemoteGameBrowser;

public struct RemoteGameFilterValue
{
	public enum ValueType
	{
		Bool,
		Int,
		String
	}

	public readonly ValueType valueType;

	public readonly int intValue;

	public readonly string stringValue;

	public bool boolValue => intValue != 0;

	public RemoteGameFilterValue(bool boolValue)
	{
		valueType = ValueType.Bool;
		intValue = (boolValue ? 1 : 0);
		stringValue = null;
	}

	public RemoteGameFilterValue(int intValue)
	{
		valueType = ValueType.Int;
		this.intValue = intValue;
		stringValue = null;
	}

	public RemoteGameFilterValue(string stringValue)
	{
		valueType = ValueType.String;
		intValue = 0;
		this.stringValue = stringValue;
	}

	public static implicit operator RemoteGameFilterValue(bool boolValue)
	{
		return new RemoteGameFilterValue(boolValue);
	}

	public static implicit operator RemoteGameFilterValue(int intValue)
	{
		return new RemoteGameFilterValue(intValue);
	}

	public static implicit operator RemoteGameFilterValue(string stringValue)
	{
		return new RemoteGameFilterValue(stringValue);
	}
}
