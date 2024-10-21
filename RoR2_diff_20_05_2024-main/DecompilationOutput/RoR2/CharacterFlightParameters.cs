using System;

namespace RoR2;

[Serializable]
public struct CharacterFlightParameters : IEquatable<CharacterFlightParameters>
{
	public int channeledFlightGranterCount;

	public bool Equals(CharacterFlightParameters other)
	{
		return channeledFlightGranterCount == other.channeledFlightGranterCount;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is CharacterFlightParameters other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return channeledFlightGranterCount;
	}

	public bool CheckShouldUseFlight()
	{
		return channeledFlightGranterCount > 0;
	}
}
