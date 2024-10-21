using System;
using System.Collections.Generic;
using System.Linq;

namespace UDPHelperSpace;

public class UDPSender
{
	public byte packageNumber = 1;

	public int maxListCount = 90;

	public List<UDPData> data = new List<UDPData>();

	public UDPSenderWork Work = new UDPSenderWork();

	public Action<byte[], int, int, byte> SendCall;

	public void Send(byte[] bytes, int numBytes, int channelId, byte error)
	{
		data.Add(new UDPData(packageNumber, bytes, numBytes, channelId, error));
	}

	public byte[] Send(byte[] bytes, ref int numBytes)
	{
		if (bytes == null || bytes.Length < 1 || (bytes.Length == 1 && bytes[0] == 0))
		{
			return bytes;
		}
		byte[] result = Work.AddDataSend(bytes, packageNumber);
		numBytes++;
		packageNumber++;
		if (packageNumber > 100)
		{
			packageNumber = 1;
		}
		return result;
	}

	public byte[] SendReturne(byte[] bytes, int numBytes, int channelId, byte error)
	{
		if (bytes == null || bytes.Length < 1 || (bytes.Length == 1 && bytes[0] == 0))
		{
			return bytes;
		}
		return new UDPData(packageNumber, bytes, numBytes, channelId, error).data;
	}

	public void SendAction()
	{
		if (data.Count > 0)
		{
			UDPData uDPData = data[data.Count - 1];
			byte[] arg = uDPData.data;
			UDPHelper.Sender.SendCall?.Invoke(arg, uDPData.numBytes, uDPData.channelId, uDPData.error);
		}
	}

	public void RemoveFromQueue(byte _packageNumber)
	{
		data = data.Where((UDPData x) => x.packageNumber != _packageNumber).ToList();
	}
}
