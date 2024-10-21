using System;
using UnityEngine.Networking;

namespace RoR2.Networking;

[Serializable]
public struct NetworkDateTime : IEquatable<NetworkDateTime>
{
	public long _binaryValue;

	public static explicit operator NetworkDateTime(DateTime dateTime)
	{
		NetworkDateTime result = default(NetworkDateTime);
		result._binaryValue = dateTime.ToBinary();
		return result;
	}

	public static explicit operator DateTime(NetworkDateTime networkDateTime)
	{
		return DateTime.FromBinary(networkDateTime._binaryValue);
	}

	public static void Serialize(in NetworkDateTime networkDateTime, NetworkWriter writer)
	{
		writer.Write(networkDateTime._binaryValue);
	}

	public static void Deserialize(out NetworkDateTime networkDateTime, NetworkReader reader)
	{
		networkDateTime._binaryValue = reader.ReadInt64();
	}

	public bool Equals(NetworkDateTime other)
	{
		return _binaryValue == other._binaryValue;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is NetworkDateTime other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _binaryValue.GetHashCode();
	}
}
