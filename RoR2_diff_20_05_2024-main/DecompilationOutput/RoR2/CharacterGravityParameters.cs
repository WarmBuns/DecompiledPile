using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct CharacterGravityParameters : IEquatable<CharacterGravityParameters>
{
	[Tooltip("AntiGravity granted by Zero-G environments. Provides AntiGravity if greater than zero and takes precedence over all other parameters.")]
	public int environmentalAntiGravityGranterCount;

	[Tooltip("AntiGravity neutralizers, like debuffs. Neutralizes non-environmental AntiGravity if greater than zero.")]
	public int antiGravityNeutralizerCount;

	[Tooltip("AntiGravity granted by body, skills, and items. Provides AntiGravity if greater than zero.")]
	public int channeledAntiGravityGranterCount;

	public bool Equals(CharacterGravityParameters other)
	{
		if (environmentalAntiGravityGranterCount == other.environmentalAntiGravityGranterCount && antiGravityNeutralizerCount == other.antiGravityNeutralizerCount)
		{
			return channeledAntiGravityGranterCount == other.channeledAntiGravityGranterCount;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is CharacterGravityParameters other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((environmentalAntiGravityGranterCount * 397) ^ antiGravityNeutralizerCount) * 397) ^ channeledAntiGravityGranterCount;
	}

	public bool CheckShouldUseGravity()
	{
		if (environmentalAntiGravityGranterCount > 0)
		{
			return false;
		}
		if (antiGravityNeutralizerCount > 0)
		{
			return true;
		}
		if (channeledAntiGravityGranterCount > 0)
		{
			return false;
		}
		return true;
	}
}
