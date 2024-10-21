using System;
using HG;

namespace RoR2;

[Serializable]
public struct DisplayRuleGroup : IEquatable<DisplayRuleGroup>
{
	public static readonly DisplayRuleGroup empty = new DisplayRuleGroup
	{
		rules = null
	};

	public ItemDisplayRule[] rules;

	public bool isEmpty
	{
		get
		{
			if (rules != null)
			{
				return rules.Length == 0;
			}
			return true;
		}
	}

	public void AddDisplayRule(ItemDisplayRule itemDisplayRule)
	{
		if (rules == null)
		{
			rules = Array.Empty<ItemDisplayRule>();
		}
		ArrayUtils.ArrayAppend(ref rules, in itemDisplayRule);
	}

	public bool Equals(DisplayRuleGroup other)
	{
		return ArrayUtils.SequenceEquals(rules, other.rules);
	}

	public override bool Equals(object obj)
	{
		if (obj is DisplayRuleGroup other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (rules == null)
		{
			return 0;
		}
		return rules.GetHashCode();
	}
}
