using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct TeamMask
{
	[SerializeField]
	public byte a;

	public static readonly TeamMask none;

	public static readonly TeamMask allButNeutral;

	public static readonly TeamMask all;

	public bool HasTeam(TeamIndex teamIndex)
	{
		if (teamIndex < TeamIndex.Neutral || teamIndex >= TeamIndex.Count)
		{
			return false;
		}
		return (a & (1L << (int)teamIndex)) != 0;
	}

	public void AddTeam(TeamIndex teamIndex)
	{
		if (teamIndex >= TeamIndex.Neutral && teamIndex < TeamIndex.Count)
		{
			a |= (byte)(1 << (int)teamIndex);
		}
	}

	public void RemoveTeam(TeamIndex teamIndex)
	{
		if (teamIndex >= TeamIndex.Neutral && teamIndex < TeamIndex.Count)
		{
			a &= (byte)(~(1 << (int)teamIndex));
		}
	}

	static TeamMask()
	{
		all = default(TeamMask);
		for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex++)
		{
			all.AddTeam(teamIndex);
		}
		allButNeutral = all;
		allButNeutral.RemoveTeam(TeamIndex.Neutral);
	}

	public static TeamMask AllExcept(TeamIndex teamIndexToExclude)
	{
		TeamMask result = all;
		result.RemoveTeam(teamIndexToExclude);
		return result;
	}

	public static TeamMask GetUnprotectedTeams(TeamIndex attackerTeam)
	{
		if (FriendlyFireManager.friendlyFireMode == FriendlyFireManager.FriendlyFireMode.Off)
		{
			return AllExcept(attackerTeam);
		}
		return all;
	}

	public static TeamMask GetEnemyTeams(TeamIndex teamIndex)
	{
		if (FriendlyFireManager.friendlyFireMode == FriendlyFireManager.FriendlyFireMode.FreeForAll)
		{
			return all;
		}
		return AllExcept(teamIndex);
	}
}
