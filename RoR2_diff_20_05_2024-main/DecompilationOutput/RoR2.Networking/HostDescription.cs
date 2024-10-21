using System;
using System.Text;
using UnityEngine.Networking;

namespace RoR2.Networking;

public struct HostDescription : IEquatable<HostDescription>
{
	public enum HostType
	{
		None,
		Self,
		Steam,
		IPv4,
		Pia,
		PS4,
		EOS,
		Playfab
	}

	public struct HostingParameters : IEquatable<HostingParameters>
	{
		public bool listen;

		public int maxPlayers;

		public bool Equals(HostingParameters other)
		{
			if (listen == other.listen)
			{
				return maxPlayers == other.maxPlayers;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is HostingParameters other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (listen.GetHashCode() * 397) ^ maxPlayers;
		}
	}

	public readonly HostType hostType;

	public readonly PlatformID userID;

	public readonly AddressPortPair addressPortPair;

	public readonly HostingParameters hostingParameters;

	public static readonly HostDescription none = new HostDescription(null);

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	public bool isRemote
	{
		get
		{
			if (hostType != 0)
			{
				return hostType != HostType.Self;
			}
			return false;
		}
	}

	public HostDescription(PlatformID id, HostType type)
	{
		this = default(HostDescription);
		hostType = type;
		userID = id;
	}

	public HostDescription(AddressPortPair addressPortPair)
	{
		this = default(HostDescription);
		hostType = HostType.IPv4;
		this.addressPortPair = addressPortPair;
	}

	public HostDescription(HostingParameters hostingParameters)
	{
		this = default(HostDescription);
		hostType = HostType.Self;
		this.hostingParameters = hostingParameters;
	}

	public bool DescribesCurrentHost()
	{
		switch (hostType)
		{
		case HostType.None:
			return !NetworkManagerSystem.singleton.isNetworkActive;
		case HostType.Self:
			if (NetworkServer.active && hostingParameters.listen != NetworkServer.dontListen)
			{
				return true;
			}
			return false;
		case HostType.Steam:
			if (NetworkManagerSystem.singleton.client?.connection is SteamNetworkConnection steamNetworkConnection && steamNetworkConnection.steamId == userID)
			{
				return true;
			}
			return false;
		case HostType.EOS:
			if (NetworkManagerSystem.singleton.client?.connection is EOSNetworkConnection eOSNetworkConnection && eOSNetworkConnection.RemoteUserID == userID.productUserID)
			{
				return true;
			}
			return false;
		case HostType.IPv4:
		{
			NetworkConnection networkConnection2 = NetworkManagerSystem.singleton.client?.connection;
			if (networkConnection2 != null && networkConnection2.address == addressPortPair.address)
			{
				return true;
			}
			return false;
		}
		case HostType.Pia:
			return false;
		case HostType.Playfab:
		{
			NetworkConnection networkConnection = NetworkManagerSystem.singleton.client?.connection;
			if (networkConnection != null && networkConnection.address == addressPortPair.address)
			{
				return true;
			}
			return false;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private HostDescription(object o)
	{
		this = default(HostDescription);
		hostType = HostType.None;
	}

	public bool Equals(HostDescription other)
	{
		if (hostType == other.hostType && userID.Equals(other.userID) && addressPortPair.Equals(other.addressPortPair))
		{
			return hostingParameters.Equals(other.hostingParameters);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is HostDescription other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((((((int)hostType * 397) ^ userID.GetHashCode()) * 397) ^ addressPortPair.GetHashCode()) * 397) ^ hostingParameters.GetHashCode();
	}

	public override string ToString()
	{
		sharedStringBuilder.Clear();
		sharedStringBuilder.Append("{ hostType=").Append(hostType);
		switch (hostType)
		{
		case HostType.Self:
			sharedStringBuilder.Append(" listen=").Append(hostingParameters.listen);
			sharedStringBuilder.Append(" maxPlayers=").Append(hostingParameters.maxPlayers);
			break;
		case HostType.Steam:
			sharedStringBuilder.Append(" steamId=").Append(userID);
			break;
		case HostType.IPv4:
			sharedStringBuilder.Append(" address=").Append(addressPortPair.address);
			sharedStringBuilder.Append(" port=").Append(addressPortPair.port);
			break;
		case HostType.EOS:
			sharedStringBuilder.Append(" steamId=").Append(userID.ToString());
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case HostType.None:
		case HostType.Pia:
		case HostType.Playfab:
			break;
		}
		sharedStringBuilder.Append(" }");
		return sharedStringBuilder.ToString();
	}
}
