using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct UserVote : IEquatable<UserVote>
{
	public GameObject networkUserObject;

	public int voteChoiceIndex;

	public bool receivedVote => voteChoiceIndex >= 0;

	public bool Equals(UserVote other)
	{
		if (other.networkUserObject.Equals(networkUserObject))
		{
			return other.voteChoiceIndex.Equals(voteChoiceIndex);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return (obj as UserVote?)?.Equals(this) ?? false;
	}

	public override int GetHashCode()
	{
		return (-555733029 * -1521134295 + EqualityComparer<GameObject>.Default.GetHashCode(networkUserObject)) * -1521134295 + voteChoiceIndex.GetHashCode();
	}
}
