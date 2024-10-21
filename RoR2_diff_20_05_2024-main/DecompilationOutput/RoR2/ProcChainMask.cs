using System;
using System.Text;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct ProcChainMask : IEquatable<ProcChainMask>
{
	[SerializeField]
	public uint mask;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	public void AddProc(ProcType procType)
	{
		mask |= (uint)(1 << (int)procType);
	}

	public void RemoveProc(ProcType procType)
	{
		mask &= (uint)(~(1 << (int)procType));
	}

	public bool HasProc(ProcType procType)
	{
		return (mask & (1 << (int)procType)) != 0;
	}

	private static bool StaticCheck()
	{
		return true;
	}

	public bool Equals(ProcChainMask other)
	{
		return mask == other.mask;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is ProcChainMask)
		{
			return Equals((ProcChainMask)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return mask.GetHashCode();
	}

	public override string ToString()
	{
		AppendToStringBuilder(sharedStringBuilder);
		string result = sharedStringBuilder.ToString();
		sharedStringBuilder.Clear();
		return result;
	}

	public void AppendToStringBuilder(StringBuilder stringBuilder)
	{
		stringBuilder.Append("(");
		bool flag = false;
		for (ProcType procType = ProcType.Behemoth; procType < ProcType.Count; procType++)
		{
			if (HasProc(procType))
			{
				if (flag)
				{
					stringBuilder.Append("|");
				}
				stringBuilder.Append(procType.ToString());
				flag = true;
			}
		}
		stringBuilder.Append(")");
	}
}
