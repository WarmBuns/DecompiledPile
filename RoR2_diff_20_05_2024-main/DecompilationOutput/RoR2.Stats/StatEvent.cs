namespace RoR2.Stats;

public struct StatEvent
{
	public enum Type
	{
		Damage,
		Kill,
		Walk,
		Die,
		Lose,
		Win
	}

	public string stringValue;

	public double doubleValue;

	public ulong ulongValue;
}
