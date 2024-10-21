namespace RoR2.Audio;

public struct AkEventIdArg
{
	public readonly uint id;

	public static explicit operator AkEventIdArg(string eventName)
	{
		return new AkEventIdArg((eventName != null) ? AkSoundEngine.GetIDFromString(eventName) : 0u);
	}

	public static implicit operator AkEventIdArg(uint akEventId)
	{
		return new AkEventIdArg(akEventId);
	}

	public static implicit operator uint(AkEventIdArg akEventIdArg)
	{
		return akEventIdArg.id;
	}

	private AkEventIdArg(uint id)
	{
		this.id = id;
	}
}
