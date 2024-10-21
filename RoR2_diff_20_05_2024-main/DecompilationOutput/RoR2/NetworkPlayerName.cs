using Facepunch.Steamworks;
using UnityEngine.Networking;

namespace RoR2;

public struct NetworkPlayerName
{
	public PlatformID playerId;

	public string nameOverride;

	public void Deserialize(NetworkReader reader)
	{
		if (reader.ReadBoolean())
		{
			playerId = PlatformID.nil;
			nameOverride = reader.ReadString();
		}
		else
		{
			playerId = new PlatformID(reader.ReadUInt64());
			nameOverride = null;
		}
	}

	public void Serialize(NetworkWriter writer)
	{
		bool flag = nameOverride != null;
		writer.Write(flag);
		if (flag)
		{
			writer.Write(nameOverride);
		}
		else
		{
			writer.Write((ulong)playerId.value);
		}
	}

	public string GetResolvedName()
	{
		if (!string.IsNullOrEmpty(nameOverride))
		{
			return (PlatformSystems.lobbyManager as EOSLobbyManager).GetUserDisplayNameFromProductIdString(nameOverride);
		}
		if (PlatformSystems.ShouldUseEpicOnlineSystems)
		{
			EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
			PlatformID user = playerId;
			return obj.GetUserDisplayName(user);
		}
		Client instance = Client.Instance;
		if (instance != null)
		{
			return instance.Friends.GetName(playerId.ID);
		}
		return "???";
	}
}
