using System;
using JetBrains.Annotations;
using UnityEngine;

[Serializable]
public class SerializableBitArray
{
	[SerializeField]
	protected readonly byte[] bytes;

	[SerializeField]
	public readonly int length;

	private const int bitMask = 7;

	public int byteCount => bytes.Length;

	public bool this[int index]
	{
		get
		{
			int num = index >> 3;
			int num2 = index & 7;
			return (bytes[num] & (1 << num2)) != 0;
		}
		set
		{
			int num = index >> 3;
			int num2 = index & 7;
			int num3 = bytes[num];
			bytes[num] = (byte)(value ? (num3 | (1 << num2)) : (num3 & ~(1 << num2)));
		}
	}

	public SerializableBitArray(int length)
	{
		bytes = new byte[length + 7 >> 3];
		this.length = length;
	}

	public SerializableBitArray(SerializableBitArray src)
	{
		if (src.bytes != null)
		{
			bytes = new byte[src.bytes.Length];
			src.bytes.CopyTo(bytes, 0);
		}
		length = src.length;
	}

	public byte[] GetBytes()
	{
		byte[] array = new byte[bytes.Length];
		GetBytes(array);
		return array;
	}

	public void GetBytes([NotNull] byte[] dest)
	{
		Buffer.BlockCopy(bytes, 0, dest, 0, bytes.Length);
	}
}
