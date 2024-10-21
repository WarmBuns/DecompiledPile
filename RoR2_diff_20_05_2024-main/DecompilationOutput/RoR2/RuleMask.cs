using JetBrains.Annotations;
using UnityEngine.Networking;

namespace RoR2;

public class RuleMask : SerializableBitArray
{
	public RuleMask()
		: base(RuleCatalog.ruleCount)
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
		if (obj is RuleMask ruleMask)
		{
			for (int i = 0; i < bytes.Length; i++)
			{
				if (bytes[i] != ruleMask.bytes[i])
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

	public void Copy([NotNull] RuleMask src)
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
