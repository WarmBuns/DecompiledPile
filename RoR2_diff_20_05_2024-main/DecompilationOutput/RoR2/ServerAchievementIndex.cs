using System;
using UnityEngine;

namespace RoR2;

public struct ServerAchievementIndex : IEquatable<ServerAchievementIndex>
{
	[SerializeField]
	public int intValue;

	public bool Equals(ServerAchievementIndex other)
	{
		return intValue == other.intValue;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is ServerAchievementIndex)
		{
			return Equals((ServerAchievementIndex)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return intValue.GetHashCode();
	}

	public static ServerAchievementIndex operator ++(ServerAchievementIndex achievementIndex)
	{
		achievementIndex.intValue++;
		return achievementIndex;
	}

	public static bool operator ==(ServerAchievementIndex a, ServerAchievementIndex b)
	{
		return a.intValue == b.intValue;
	}

	public static bool operator !=(ServerAchievementIndex a, ServerAchievementIndex b)
	{
		return a.intValue != b.intValue;
	}
}
