using UnityEngine.Networking;

namespace RoR2.Networking;

public static class UNETExtensions
{
	public static void ForceInitialize(this NetworkConnection conn, HostTopology hostTopology)
	{
		int num = 0;
		conn.Initialize("localhost", num, num, hostTopology);
	}
}
