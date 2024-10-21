public class UDPTest
{
	private byte counter = 1;

	public void AddByte(ref byte[] bytes, ref int numBytes, ref bool shouldAddToQueue, int channelId)
	{
		if (numBytes == 1 && bytes[0] == 0)
		{
			return;
		}
		if (bytes.Length <= numBytes)
		{
			byte[] array = new byte[numBytes + 1];
			for (int i = 0; i < bytes.Length; i++)
			{
				array[i] = bytes[i];
			}
			bytes = array;
		}
		shouldAddToQueue = TryAdd(ref bytes, ref numBytes, channelId);
	}

	private bool TryAdd(ref byte[] data, ref int numbytes, int channelId)
	{
		if (true)
		{
			data[numbytes] = GetCounter();
		}
		else
		{
			data[numbytes] = 0;
		}
		numbytes++;
		return true;
	}

	private byte GetCounter()
	{
		return 7;
	}
}
