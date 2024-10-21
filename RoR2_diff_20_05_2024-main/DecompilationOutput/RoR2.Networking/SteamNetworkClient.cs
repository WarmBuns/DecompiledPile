using UnityEngine.Networking;

namespace RoR2.Networking;

public class SteamNetworkClient : NetworkClient
{
	public SteamNetworkConnection steamConnection => (SteamNetworkConnection)base.connection;

	public string status => m_AsyncConnect.ToString();

	public void Connect()
	{
		Connect("localhost", 0);
		m_AsyncConnect = ConnectState.Connected;
		base.connection.ForceInitialize(base.hostTopology);
	}

	public SteamNetworkClient(NetworkConnection conn)
		: base(conn)
	{
		SetNetworkConnectionClass<SteamNetworkConnection>();
	}
}
