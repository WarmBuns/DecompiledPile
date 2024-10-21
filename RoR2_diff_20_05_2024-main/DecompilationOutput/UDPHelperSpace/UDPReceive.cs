using System.Collections.Generic;

namespace UDPHelperSpace;

public class UDPReceive
{
	public int packageNumber;

	public List<UDPData> data = new List<UDPData>();

	public UDPReceiveWork Work = new UDPReceiveWork();

	public byte[] Receive(byte[] bytes, int length)
	{
		if (bytes == null || bytes.Length < 1 || (bytes.Length == 1 && bytes[0] == 0))
		{
			return bytes;
		}
		byte b = 0;
		byte ñonfirmationByte = 0;
		byte packetLoss = 0;
		byte temp = 0;
		byte[] array = Work.GetData(bytes, out b, out ñonfirmationByte, out packetLoss, out temp, length);
		string.Concat(string.Concat(string.Concat("_packageNumber: " + b + " ", "ñonfirmationByte: ", ñonfirmationByte.ToString(), " "), "packetLoss: ", packetLoss.ToString(), "\n"), "total bytes: ", bytes.Length.ToString(), "\n");
		if (packageNumber == 0)
		{
			packageNumber = b;
			ConfirmPackage(b);
			return array;
		}
		if (length == 4)
		{
			UDPHelper.Sender.RemoveFromQueue(array[2]);
			return null;
		}
		if (packageNumber == b)
		{
			ConfirmPackage(b);
			return null;
		}
		if (b == packageNumber + 1 || (b == 1 && packageNumber == 100))
		{
			packageNumber = b;
			ConfirmPackage(b);
		}
		return array;
	}

	public void ConfirmPackage(byte confirmPackageNumber)
	{
		byte[] arg = new byte[4] { 1, 0, 0, confirmPackageNumber };
		UDPHelper.Sender.SendCall?.Invoke(arg, 4, 0, 0);
	}
}
