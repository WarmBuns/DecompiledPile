using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct ToStringImplementationInvariant : IToStringImplementation<int>, IToStringImplementation<uint>, IToStringImplementation<long>, IToStringImplementation<ulong>, IToStringImplementation<short>, IToStringImplementation<ushort>, IToStringImplementation<float>, IToStringImplementation<double>
{
	public string DoToString(in int input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in uint input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in long input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in ulong input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in short input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in ushort input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in float input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in double input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	public string DoToString(in decimal input)
	{
		return TextSerialization.ToStringInvariant(input);
	}

	string IToStringImplementation<int>.DoToString(in int input)
	{
		return DoToString(in input);
	}

	string IToStringImplementation<uint>.DoToString(in uint input)
	{
		return DoToString(in input);
	}

	string IToStringImplementation<long>.DoToString(in long input)
	{
		return DoToString(in input);
	}

	string IToStringImplementation<ulong>.DoToString(in ulong input)
	{
		return DoToString(in input);
	}

	string IToStringImplementation<short>.DoToString(in short input)
	{
		return DoToString(in input);
	}

	string IToStringImplementation<ushort>.DoToString(in ushort input)
	{
		return DoToString(in input);
	}

	string IToStringImplementation<float>.DoToString(in float input)
	{
		return DoToString(in input);
	}

	string IToStringImplementation<double>.DoToString(in double input)
	{
		return DoToString(in input);
	}
}
