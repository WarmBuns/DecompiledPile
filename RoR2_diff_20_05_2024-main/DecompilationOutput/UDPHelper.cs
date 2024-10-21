using UDPHelperSpace;
using UnityEngine;

public static class UDPHelper
{
	public static UDPSender Sender;

	public static UDPReceive Receiver;

	public static UDPTest Test;

	private static UDPSenderUpdate uDPSender;

	static UDPHelper()
	{
		Sender = new UDPSender();
		Receiver = new UDPReceive();
		Test = new UDPTest();
		GameObject gameObject = new GameObject("UDPHelperSender");
		gameObject.AddComponent<UDPSenderUpdate>();
		uDPSender = gameObject.GetComponent<UDPSenderUpdate>();
	}
}
