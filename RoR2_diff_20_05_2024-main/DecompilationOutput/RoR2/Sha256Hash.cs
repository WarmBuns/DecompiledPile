using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace RoR2;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct Sha256Hash : IEquatable<Sha256Hash>
{
	[SerializeField]
	private ulong _00_07;

	[SerializeField]
	private ulong _08_15;

	[SerializeField]
	private ulong _16_23;

	[SerializeField]
	private ulong _24_31;

	private static readonly byte[] ulongToByteBuffer = new byte[8];

	public static Sha256Hash FromHexString(string hexString, int startIndex = 0)
	{
		if (startIndex + hexString.Length < 64)
		{
			throw new ArgumentException("Not enough characters in string.");
		}
		int readPos = startIndex;
		ulong num = ReadULong();
		ulong num2 = ReadULong();
		ulong num3 = ReadULong();
		ulong num4 = ReadULong();
		return FromULong4(num, num2, num3, num4);
		byte ReadByte()
		{
			byte num5 = ReadNibble();
			byte b = ReadNibble();
			return (byte)((num5 << 4) | b);
		}
		byte ReadNibble()
		{
			char c = hexString[readPos++];
			if (c >= '0' && c <= '9')
			{
				return (byte)(c - 48);
			}
			if (c >= 'a' && c <= 'f')
			{
				return (byte)(10 + (c - 97));
			}
			if (c >= 'A' && c <= 'F')
			{
				return (byte)(10 + (c - 65));
			}
			return 0;
		}
		ulong ReadULong()
		{
			ulongToByteBuffer[0] = ReadByte();
			ulongToByteBuffer[1] = ReadByte();
			ulongToByteBuffer[2] = ReadByte();
			ulongToByteBuffer[3] = ReadByte();
			ulongToByteBuffer[4] = ReadByte();
			ulongToByteBuffer[5] = ReadByte();
			ulongToByteBuffer[6] = ReadByte();
			ulongToByteBuffer[7] = ReadByte();
			return BitConverter.ToUInt64(ulongToByteBuffer, 0);
		}
	}

	public static Sha256Hash FromULong4(ulong _00_07, ulong _08_15, ulong _16_23, ulong _24_31)
	{
		Sha256Hash result = default(Sha256Hash);
		result._00_07 = _00_07;
		result._08_15 = _08_15;
		result._16_23 = _16_23;
		result._24_31 = _24_31;
		return result;
	}

	public void ToULong4(out ulong _00_07, out ulong _08_15, out ulong _16_23, out ulong _24_31)
	{
		_00_07 = this._00_07;
		_08_15 = this._08_15;
		_16_23 = this._16_23;
		_24_31 = this._24_31;
	}

	public static Sha256Hash FromBytes(byte[] bytes, int startIndex = 0)
	{
		if (startIndex + bytes.Length < 32)
		{
			throw new ArgumentException("Not enough bytes in buffer.");
		}
		Sha256Hash result = default(Sha256Hash);
		result._00_07 = BitConverter.ToUInt64(bytes, startIndex);
		result._08_15 = BitConverter.ToUInt64(bytes, startIndex + 8);
		result._16_23 = BitConverter.ToUInt64(bytes, startIndex + 16);
		result._24_31 = BitConverter.ToUInt64(bytes, startIndex + 24);
		return result;
	}

	public bool Equals(Sha256Hash other)
	{
		if (_00_07 == other._00_07 && _08_15 == other._08_15 && _16_23 == other._16_23)
		{
			return _24_31 == other._24_31;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Sha256Hash other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((_00_07.GetHashCode() * 397) ^ _08_15.GetHashCode()) * 397) ^ _16_23.GetHashCode()) * 397) ^ _24_31.GetHashCode();
	}

	public static StringBuilder AppendSha256HashHex(StringBuilder stringBuilder, in Sha256Hash hash)
	{
		stringBuilder.EnsureCapacity(stringBuilder.Length + 64);
		AppendULong(hash._00_07);
		AppendULong(hash._08_15);
		AppendULong(hash._16_23);
		AppendULong(hash._24_31);
		return stringBuilder;
		void AppendULong(ulong value)
		{
			stringBuilder.AppendByteHexValue((byte)(value & 0xFF));
			stringBuilder.AppendByteHexValue((byte)((value >> 8) & 0xFF));
			stringBuilder.AppendByteHexValue((byte)((value >> 16) & 0xFF));
			stringBuilder.AppendByteHexValue((byte)((value >> 24) & 0xFF));
			stringBuilder.AppendByteHexValue((byte)((value >> 32) & 0xFF));
			stringBuilder.AppendByteHexValue((byte)((value >> 40) & 0xFF));
			stringBuilder.AppendByteHexValue((byte)((value >> 48) & 0xFF));
			stringBuilder.AppendByteHexValue((byte)((value >> 56) & 0xFF));
		}
	}

	public override string ToString()
	{
		return AppendSha256HashHex(new StringBuilder(64), in this).ToString();
	}
}
