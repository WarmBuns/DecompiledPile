using System;
using UnityEngine.Networking;

namespace RoR2.Networking;

public static class NetworkExtensions
{
	public static void Write(this NetworkWriter writer, in NetworkDateTime networkDateTime)
	{
		NetworkDateTime.Serialize(in networkDateTime, writer);
	}

	public static NetworkDateTime ReadNetworkDateTime(this NetworkReader reader)
	{
		NetworkDateTime.Deserialize(out var networkDateTime, reader);
		return networkDateTime;
	}

	public static void WriteNetworkGuid(this NetworkWriter networkWriter, in NetworkGuid guid)
	{
		guid.Serialize(networkWriter);
	}

	public static void WriteGuid(this NetworkWriter networkWriter, in Guid guid)
	{
		networkWriter.WriteNetworkGuid((NetworkGuid)guid);
	}

	public static NetworkGuid ReadNetworkGuid(this NetworkReader networkReader)
	{
		NetworkGuid result = default(NetworkGuid);
		result.Deserialize(networkReader);
		return result;
	}

	public static Guid ReadGuid(this NetworkReader networkReader)
	{
		return (Guid)networkReader.ReadNetworkGuid();
	}
}
