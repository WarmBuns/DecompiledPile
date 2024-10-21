namespace UDPHelperSpace;

public class UDPData
{
	public byte packageNumber = 2;

	public byte[] data;

	public int numBytes;

	public int channelId;

	public byte error;

	public UDPData()
	{
	}

	public UDPData(byte packageNumber, byte[] data, int numBytes, int channelId, byte error)
	{
		this.packageNumber = packageNumber;
		this.packageNumber = data[0];
		this.data = data;
		this.numBytes = numBytes;
		this.channelId = channelId;
		this.error = error;
	}
}
