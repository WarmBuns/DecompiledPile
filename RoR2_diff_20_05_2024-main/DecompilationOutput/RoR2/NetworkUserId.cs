using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct NetworkUserId : IEquatable<NetworkUserId>
{
	[SerializeField]
	public ulong value;

	[SerializeField]
	public string strValue;

	[SerializeField]
	public readonly byte subId;

	public PlatformID steamId => new PlatformID(value);

	private NetworkUserId(ulong value, byte subId)
	{
		this.value = value;
		this.subId = subId;
		strValue = null;
	}

	public NetworkUserId(string strValue, byte subId)
	{
		value = 0uL;
		this.subId = subId;
		this.strValue = strValue;
	}

	public NetworkUserId(PlatformID userId, byte subId)
	{
		if (userId.isSteam)
		{
			value = userId.ID;
			strValue = null;
		}
		else
		{
			strValue = userId.stringID;
			value = 0uL;
		}
		this.subId = subId;
	}

	public static NetworkUserId FromIp(string ip, byte subId)
	{
		return new NetworkUserId((ulong)ip.GetHashCode(), subId);
	}

	public static NetworkUserId FromId(ulong id, byte subId)
	{
		return new NetworkUserId(id, subId);
	}

	public bool HasValidValue()
	{
		if (value == 0L)
		{
			return strValue != null;
		}
		return true;
	}

	public bool Equals(NetworkUserId other)
	{
		if (value == other.value && strValue == other.strValue)
		{
			return subId == other.subId;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is NetworkUserId)
		{
			return Equals((NetworkUserId)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (value.GetHashCode() * 397) ^ subId.GetHashCode();
	}
}
