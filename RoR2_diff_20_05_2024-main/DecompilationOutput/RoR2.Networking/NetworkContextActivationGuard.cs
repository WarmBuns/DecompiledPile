using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class NetworkContextActivationGuard : MonoBehaviour
{
	public enum Rule
	{
		Neutral,
		MustBeTrue,
		MustBeFalse
	}

	public Rule server;

	public Rule client;

	private void Awake()
	{
		bool flag = true;
		flag &= CheckRule(server, NetworkServer.active);
		flag &= CheckRule(client, NetworkClient.active);
		base.gameObject.SetActive(flag);
	}

	private bool CheckRule(Rule rule, bool value)
	{
		return rule switch
		{
			Rule.Neutral => true, 
			Rule.MustBeTrue => value, 
			Rule.MustBeFalse => !value, 
			_ => false, 
		};
	}
}
