using System;
using System.Linq;

namespace UDPHelperSpace;

public class UDPReceiveWork
{
	public byte[] GetData(byte[] bytes, out byte packageNumber, out byte ñonfirmationByte, out byte packetLoss, out byte temp2, int length)
	{
		byte[] array = new byte[bytes.Length];
		Array.Copy(bytes, array, bytes.Length);
		ñonfirmationByte = 0;
		packetLoss = 0;
		temp2 = 0;
		packageNumber = bytes[0];
		return RemoveDataReceive(array);
	}

	private byte[] RemoveDataReceive(byte[] bytes)
	{
		return RemoveBytes(bytes);
	}

	private byte[] RemoveBytes(byte[] bytes, int countRemove = 1)
	{
		return bytes = bytes.Skip(1).ToArray();
	}
}
