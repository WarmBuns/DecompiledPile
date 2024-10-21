using System;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RoR2.Networking;

[Serializable]
public struct NetworkGuid : IEquatable<NetworkGuid>
{
	[StructLayout(LayoutKind.Explicit)]
	private struct ConverterUnion
	{
		[FieldOffset(0)]
		public Guid guildValue;

		[FieldOffset(0)]
		public NetworkGuid networkGuidValue;
	}

	public ulong _a;

	public ulong _b;

	public static explicit operator NetworkGuid(Guid guid)
	{
		ConverterUnion converterUnion = default(ConverterUnion);
		converterUnion.guildValue = guid;
		return converterUnion.networkGuidValue;
	}

	public static explicit operator Guid(NetworkGuid networkGuid)
	{
		ConverterUnion converterUnion = default(ConverterUnion);
		converterUnion.networkGuidValue = networkGuid;
		return converterUnion.guildValue;
	}

	public bool Equals(NetworkGuid other)
	{
		if (_a == other._a)
		{
			return _b == other._b;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is NetworkGuid other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (_a.GetHashCode() * 397) ^ _b.GetHashCode();
	}

	public static bool operator ==(NetworkGuid left, NetworkGuid right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(NetworkGuid left, NetworkGuid right)
	{
		return !left.Equals(right);
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.Write(_a);
		writer.Write(_b);
	}

	public void Deserialize(NetworkReader reader)
	{
		_a = reader.ReadUInt64();
		_b = reader.ReadUInt64();
	}
}
