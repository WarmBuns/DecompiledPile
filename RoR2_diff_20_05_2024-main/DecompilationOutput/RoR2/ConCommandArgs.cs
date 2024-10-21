using System;
using System.Collections.Generic;
using RoR2.Networking;
using UnityEngine;

namespace RoR2;

public struct ConCommandArgs
{
	public List<string> userArgs;

	public NetworkUser sender;

	public LocalUser localUserSender;

	public string commandName;

	public string this[int i] => userArgs[i];

	public int Count => userArgs.Count;

	public GameObject senderMasterObject
	{
		get
		{
			if (!sender)
			{
				return null;
			}
			return sender.masterObject;
		}
	}

	public CharacterMaster senderMaster
	{
		get
		{
			if (!sender)
			{
				return null;
			}
			return sender.master;
		}
	}

	public CharacterBody senderBody
	{
		get
		{
			if (!sender)
			{
				return null;
			}
			return sender.GetCurrentBody();
		}
	}

	public void CheckArgumentCount(int count)
	{
		ConCommandException.CheckArgumentCount(userArgs, count);
	}

	public string TryGetArgString(int index)
	{
		if ((uint)index < (uint)userArgs.Count)
		{
			return userArgs[index];
		}
		return null;
	}

	public string GetArgString(int index)
	{
		return TryGetArgString(index) ?? throw new ConCommandException($"Argument {index} must be a string.");
	}

	public ulong? TryGetArgUlong(int index)
	{
		if ((uint)index < (uint)userArgs.Count && TextSerialization.TryParseInvariant(userArgs[index], out ulong result))
		{
			return result;
		}
		return null;
	}

	public ulong GetArgULong(int index)
	{
		ulong? num = TryGetArgUlong(index);
		if (!num.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be an unsigned integer.");
		}
		return num.Value;
	}

	public int? TryGetArgInt(int index)
	{
		if ((uint)index < (uint)userArgs.Count && TextSerialization.TryParseInvariant(userArgs[index], out int result))
		{
			return result;
		}
		return null;
	}

	public int GetArgInt(int index)
	{
		int? num = TryGetArgInt(index);
		if (!num.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be an integer.");
		}
		return num.Value;
	}

	public bool? TryGetArgBool(int index)
	{
		int? num = TryGetArgInt(index);
		if (num.HasValue)
		{
			return num > 0;
		}
		return null;
	}

	public bool GetArgBool(int index)
	{
		int? num = TryGetArgInt(index);
		if (!num.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be a boolean.");
		}
		return num.Value > 0;
	}

	public float? TryGetArgFloat(int index)
	{
		if ((uint)index < (uint)userArgs.Count && TextSerialization.TryParseInvariant(userArgs[index], out float result))
		{
			return result;
		}
		return null;
	}

	public float GetArgFloat(int index)
	{
		float? num = TryGetArgFloat(index);
		if (!num.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be a number.");
		}
		return num.Value;
	}

	public double? TryGetArgDouble(int index)
	{
		if ((uint)index < (uint)userArgs.Count && TextSerialization.TryParseInvariant(userArgs[index], out double result))
		{
			return result;
		}
		return null;
	}

	public double GetArgDouble(int index)
	{
		double? num = TryGetArgDouble(index);
		if (!num.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be a number.");
		}
		return num.Value;
	}

	public T? TryGetArgEnum<T>(int index) where T : struct
	{
		if ((uint)index < (uint)userArgs.Count && Enum.TryParse<T>(userArgs[index], ignoreCase: true, out var result))
		{
			return result;
		}
		return null;
	}

	public T GetArgEnum<T>(int index) where T : struct
	{
		T? val = TryGetArgEnum<T>(index);
		if (!val.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be one of the values of {typeof(T).Name}.");
		}
		return val.Value;
	}

	public PlatformID? TryGetArgPlatformID(int index)
	{
		if ((uint)index < (uint)userArgs.Count && PlatformID.TryParse(userArgs[index], out var result))
		{
			return result;
		}
		return null;
	}

	public PlatformID GetArgPlatformID(int index)
	{
		PlatformID? platformID = TryGetArgPlatformID(index);
		if (!platformID.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be a valid Steam ID.");
		}
		return platformID.Value;
	}

	public AddressPortPair? TryGetArgAddressPortPair(int index)
	{
		if ((uint)index < (uint)userArgs.Count && AddressPortPair.TryParse(userArgs[index], out var addressPortPair))
		{
			return addressPortPair;
		}
		return null;
	}

	public AddressPortPair GetArgAddressPortPair(int index)
	{
		AddressPortPair? addressPortPair = TryGetArgAddressPortPair(index);
		if (!addressPortPair.HasValue)
		{
			throw new ConCommandException($"Argument {index} must be a valid address and port pair in the format \"address:port\" (e.g. \"127.0.0.1:27015\"). Given value: {TryGetArgString(index)}");
		}
		return addressPortPair.Value;
	}

	public PickupIndex GetArgPickupIndex(int index)
	{
		PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(TryGetArgString(index) ?? string.Empty);
		if (pickupIndex == PickupIndex.none)
		{
			throw new ConCommandException($"Argument {index} must be a valid pickup name.");
		}
		return pickupIndex;
	}

	public LocalUser GetSenderLocalUser()
	{
		return localUserSender ?? throw new ConCommandException($"Command requires a local user that is not available.");
	}

	public CharacterBody TryGetSenderBody()
	{
		if (!sender)
		{
			return null;
		}
		return sender.GetCurrentBody();
	}

	public CharacterBody GetSenderBody()
	{
		CharacterBody characterBody = TryGetSenderBody();
		if ((bool)characterBody)
		{
			return characterBody;
		}
		throw new ConCommandException("Command requires the sender to have a body.");
	}

	public CharacterMaster GetSenderMaster()
	{
		if (!senderMaster)
		{
			throw new ConCommandException("Command requires the sender to have a CharacterMaster. The game must be in an active run and the sender must be a participating player.");
		}
		return senderMaster;
	}

	public void Log(string message)
	{
	}
}
