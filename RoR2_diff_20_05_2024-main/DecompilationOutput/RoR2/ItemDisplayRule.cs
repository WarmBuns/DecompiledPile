using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct ItemDisplayRule : IEquatable<ItemDisplayRule>
{
	public ItemDisplayRuleType ruleType;

	public GameObject followerPrefab;

	public string childName;

	public Vector3 localPos;

	public Vector3 localAngles;

	public Vector3 localScale;

	public LimbFlags limbMask;

	public bool Equals(ItemDisplayRule other)
	{
		if (ruleType == other.ruleType && object.Equals(followerPrefab, other.followerPrefab) && string.Equals(childName, other.childName) && localPos.Equals(other.localPos) && localAngles.Equals(other.localAngles) && localScale.Equals(other.localScale))
		{
			return limbMask == other.limbMask;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ItemDisplayRule other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((((((((((((int)ruleType * 397) ^ ((followerPrefab != null) ? followerPrefab.GetHashCode() : 0)) * 397) ^ ((childName != null) ? childName.GetHashCode() : 0)) * 397) ^ localPos.GetHashCode()) * 397) ^ localAngles.GetHashCode()) * 397) ^ localScale.GetHashCode()) * 397) ^ (int)limbMask;
	}
}
