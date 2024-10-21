using UnityEngine.Networking;

namespace RoR2.Networking;

public class EOSNetworkClient : NetworkClient
{
	public EOSNetworkConnection eosConnection => (EOSNetworkConnection)base.connection;

	public string status => m_AsyncConnect.ToString();

	public void Connect()
	{
		Connect("localhost", 0);
		m_AsyncConnect = ConnectState.Connected;
		base.connection.ForceInitialize(base.hostTopology);
	}

	public EOSNetworkClient(NetworkConnection conn)
		: base(conn)
	{
		SetNetworkConnectionClass<EOSNetworkConnection>();
	}
}
