using System;
using System.Text;

[Serializable]
internal class ApplicationDataStruct
{
	public uint quickPlayCutoffTime;

	public string hostName = "";

	public int Serialize(byte[] destinationBuf)
	{
		int offset = 0;
		EncodeUint(quickPlayCutoffTime, ref offset, ref destinationBuf);
		EncodeString(hostName, ref offset, ref destinationBuf);
		return offset;
	}

	private void EncodeUint(uint u, ref int offset, ref byte[] destinationBuf)
	{
		BitConverter.GetBytes(u).CopyTo(destinationBuf, offset);
		offset += 4;
	}

	private void EncodeString(string s, ref int offset, ref byte[] destinationBuf)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(s);
		byte[] bytes2 = BitConverter.GetBytes(bytes.Length);
		bytes2.CopyTo(destinationBuf, offset);
		offset += bytes2.Length;
		bytes.CopyTo(destinationBuf, offset);
		offset += bytes.Length;
	}

	public void Deserialize(byte[] buffer, uint dataSize)
	{
		int offset = 0;
		quickPlayCutoffTime = DecodeUint(ref offset, buffer);
		hostName = DecodeString(ref offset, buffer);
	}

	private uint DecodeUint(ref int offset, byte[] buffer)
	{
		uint result = BitConverter.ToUInt32(buffer, offset);
		offset += 4;
		return result;
	}

	private string DecodeString(ref int offset, byte[] buffer)
	{
		string result = null;
		int num = BitConverter.ToInt32(buffer, offset);
		offset += 4;
		if (num > 0)
		{
			result = Encoding.ASCII.GetString(buffer, offset, num);
			offset += num;
		}
		return result;
	}
}
