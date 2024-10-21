namespace UDPHelperSpace;

public class UDPSenderWork
{
	public byte[] AddDataSend(byte[] bytes, int packageNumber)
	{
		if (bytes == null || bytes.Length < 4)
		{
			return bytes;
		}
		bytes = AddByteToArray(bytes, packageNumber);
		return bytes;
	}

	private byte[] AddByteToArray(byte[] bArray, int newByte)
	{
		return AddByteToArray(bArray, (byte)newByte);
	}

	public byte[] AddByteToArray(byte[] bArray, byte newByte)
	{
		byte[] array = new byte[bArray.Length + 1];
		bArray.CopyTo(array, 1);
		array[0] = newByte;
		return array;
	}
}
