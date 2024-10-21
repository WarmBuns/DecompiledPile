using JetBrains.Annotations;
using UnityEngine.Networking;

namespace RoR2;

public class RuleChoiceMask : SerializableBitArray
{
	public bool this[RuleChoiceDef choiceDef]
	{
		get
		{
			return base[choiceDef.globalIndex];
		}
		set
		{
			base[choiceDef.globalIndex] = value;
		}
	}

	public RuleChoiceMask()
		: base(RuleCatalog.choiceCount)
	{
	}

	public void Serialize(NetworkWriter writer)
	{
		for (int i = 0; i < bytes.Length; i++)
		{
			writer.Write(bytes[i]);
		}
	}

	public void Deserialize(NetworkReader reader)
	{
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = reader.ReadByte();
		}
	}

	public override bool Equals(object obj)
	{
		if (obj is RuleChoiceMask ruleChoiceMask)
		{
			for (int i = 0; i < bytes.Length; i++)
			{
				if (bytes[i] != ruleChoiceMask.bytes[i])
				{
					return false;
				}
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		int num = 0;
		for (int i = 0; i < bytes.Length; i++)
		{
			num += bytes[i];
		}
		return num;
	}

	public void Copy([NotNull] RuleChoiceMask src)
	{
		byte[] array = src.bytes;
		byte[] array2 = bytes;
		int i = 0;
		for (int num = array2.Length; i < num; i++)
		{
			array2[i] = array[i];
		}
	}
}
