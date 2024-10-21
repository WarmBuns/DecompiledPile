using System;
using System.Globalization;
using System.Net;

namespace RoR2.Networking;

public struct AddressPortPair : IEquatable<AddressPortPair>
{
	public string address;

	public ushort port;

	public bool isValid => !string.IsNullOrEmpty(address);

	public AddressPortPair(string address, ushort port)
	{
		this.address = address;
		this.port = port;
	}

	public AddressPortPair(IPAddress address, ushort port)
	{
		this.address = address.ToString();
		this.port = port;
	}

	public static bool TryParse(string str, out AddressPortPair addressPortPair)
	{
		if (!string.IsNullOrEmpty(str))
		{
			int num = str.Length - 1;
			while (num >= 0 && str[num] != ':')
			{
				num--;
			}
			if (num >= 0)
			{
				string text = str.Substring(0, num);
				string s = str.Substring(num + 1, str.Length - num - 1);
				addressPortPair.address = text;
				addressPortPair.port = (ushort)(TextSerialization.TryParseInvariant(s, out ushort result) ? result : 0);
				return true;
			}
		}
		addressPortPair.address = "";
		addressPortPair.port = 0;
		return false;
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", address, port);
	}

	public bool Equals(AddressPortPair other)
	{
		if (string.Equals(address, other.address))
		{
			return port == other.port;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is AddressPortPair other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((address != null) ? address.GetHashCode() : 0) * 397) ^ port.GetHashCode();
	}
}
