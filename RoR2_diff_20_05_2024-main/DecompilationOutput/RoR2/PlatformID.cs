using System;
using System.Globalization;
using Epic.OnlineServices;

namespace RoR2;

public struct PlatformID : IEquatable<PlatformID>
{
	public enum EAccountType
	{
		k_EAccountTypeInvalid,
		k_EAccountTypeIndividual,
		k_EAccountTypeMultiseat,
		k_EAccountTypeGameServer,
		k_EAccountTypeAnonGameServer,
		k_EAccountTypePending,
		k_EAccountTypeContentServer,
		k_EAccountTypeClan,
		k_EAccountTypeChat,
		k_EAccountTypeConsoleUser,
		k_EAccountTypeAnonUser,
		k_EAccountTypeMax
	}

	public enum EUniverse
	{
		k_EUniverseInvalid,
		k_EUniversePublic,
		k_EUniverseBeta,
		k_EUniverseInternal,
		k_EUniverseDev,
		k_EUniverseMax
	}

	public ulong ID;

	public ulong ID2;

	public string stringID;

	public static readonly PlatformID nil = new PlatformID(0uL, string.Empty);

	public object value
	{
		get
		{
			if (ID != 0L)
			{
				return ID;
			}
			if (stringID != string.Empty)
			{
				return stringID;
			}
			return null;
		}
	}

	public bool isValid => isSteam ^ isEGS;

	public bool isSteam => ID != 0;

	public bool isEGS => !string.IsNullOrEmpty(stringID);

	public ProductUserId productUserID => ProductUserId.FromString(stringID);

	public uint accountId => GetBitField(0, 32);

	public uint accountInstance => GetBitField(32, 20);

	public EAccountType accountType => (EAccountType)GetBitField(52, 4);

	public EUniverse universe => (EUniverse)GetBitField(56, 8);

	public bool isLobby => accountType == EAccountType.k_EAccountTypeChat;

	public PlatformID(ulong id)
	{
		ID = id;
		ID2 = ID;
		stringID = string.Empty;
	}

	public PlatformID(string stringid)
	{
		ID = 0uL;
		ID2 = ID;
		stringID = stringid;
	}

	public PlatformID(ulong id, string stringid)
	{
		ID = id;
		ID2 = ID;
		stringID = stringid;
	}

	public static explicit operator PlatformID(ulong id)
	{
		return new PlatformID(id);
	}

	public static bool operator ==(PlatformID first, PlatformID second)
	{
		return first.Compare(second);
	}

	public static bool operator !=(PlatformID first, PlatformID second)
	{
		return !first.Compare(second);
	}

	public static bool operator ==(PlatformID first, ulong second)
	{
		return first.ID == second;
	}

	public static bool operator !=(PlatformID first, ulong second)
	{
		return first.ID != second;
	}

	public override string ToString()
	{
		if (value == null)
		{
			return "";
		}
		return value.ToString();
	}

	public bool IsDefault()
	{
		return ID == 0;
	}

	public bool Equals(PlatformID other)
	{
		return Compare(other);
	}

	public bool Compare(PlatformID other)
	{
		if (value is ulong)
		{
			if (other.value is ulong)
			{
				return (ulong)value == (ulong)other.value;
			}
			return false;
		}
		if (value is string)
		{
			if (other.value is string)
			{
				return (string)value == (string)other.value;
			}
			return false;
		}
		if (value == null)
		{
			return other.value == null;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (value != null)
		{
			return value.GetHashCode();
		}
		return 0;
	}

	public PlatformID(ProductUserId id)
	{
		stringID = id.ToString();
		ID = 0uL;
		ID2 = 0uL;
	}

	public static bool TryParse(string str, out PlatformID result)
	{
		if (!string.IsNullOrEmpty(str))
		{
			return ParseFromString(str, out result);
		}
		result = nil;
		return false;
	}

	private static bool ParseFromString(string str, out PlatformID result)
	{
		if (str[0] <= '9')
		{
			ulong result2;
			bool flag = TextSerialization.TryParseInvariant(str, out result2);
			if (flag)
			{
				result = new PlatformID(flag ? result2 : 0);
				return flag;
			}
		}
		if (PlatformSystems.EgsToggleConVar.value == 1 && str.Length <= 32)
		{
			ProductUserId productUserId = ProductUserId.FromString(str);
			if (productUserId != null)
			{
				result = new PlatformID(productUserId);
				return true;
			}
		}
		result = default(PlatformID);
		return false;
	}

	private uint GetBitField(int bitOffset, int bitCount)
	{
		uint num = uint.MaxValue >> 32 - bitCount;
		return (uint)(int)(ID >> bitOffset) & num;
	}

	public string ToSteamID()
	{
		uint num = accountId;
		return string.Format(CultureInfo.InvariantCulture, "STEAM_{0}:{1}:{2}", (uint)universe, num & 1u, num >> 1);
	}

	public string ToCommunityID()
	{
		char c = 'I';
		switch (accountType)
		{
		case EAccountType.k_EAccountTypeInvalid:
			c = 'I';
			break;
		case EAccountType.k_EAccountTypeIndividual:
			c = 'U';
			break;
		case EAccountType.k_EAccountTypeMultiseat:
			c = 'M';
			break;
		case EAccountType.k_EAccountTypeGameServer:
			c = 'G';
			break;
		case EAccountType.k_EAccountTypeAnonGameServer:
			c = 'A';
			break;
		case EAccountType.k_EAccountTypePending:
			c = 'P';
			break;
		case EAccountType.k_EAccountTypeContentServer:
			c = 'C';
			break;
		case EAccountType.k_EAccountTypeClan:
			c = 'g';
			break;
		case EAccountType.k_EAccountTypeChat:
			c = 'T';
			break;
		case EAccountType.k_EAccountTypeConsoleUser:
			c = 'I';
			break;
		case EAccountType.k_EAccountTypeAnonUser:
			c = 'a';
			break;
		case EAccountType.k_EAccountTypeMax:
			c = 'I';
			break;
		}
		return string.Format(CultureInfo.InvariantCulture, "[{0}:{1}:{2}]", c, 1, accountId);
	}
}
