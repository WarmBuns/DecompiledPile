namespace RoR2.WwiseUtils;

public struct StateSetter
{
	private readonly string name;

	private readonly uint id;

	private uint expectedEngineValueId;

	public uint valueId;

	public StateSetter(string name)
	{
		this.name = name;
		id = AkSoundEngine.GetIDFromString(name);
		expectedEngineValueId = 0u;
		valueId = expectedEngineValueId;
	}

	public void FlushIfChanged()
	{
		if (!expectedEngineValueId.Equals(valueId))
		{
			expectedEngineValueId = valueId;
			AkSoundEngine.SetState(id, valueId);
		}
	}
}
