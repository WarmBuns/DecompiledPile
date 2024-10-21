using System;

public struct MemoizedToString<TInput, TToString> where TInput : IEquatable<TInput> where TToString : struct, IToStringImplementation<TInput>
{
	private TInput lastInput;

	private string lastOutput;

	private TToString implementation;

	private static readonly MemoizedToString<TInput, TToString> defaultValue;

	public string GetString(in TInput input)
	{
		if (!input.Equals(lastInput))
		{
			lastInput = input;
			lastOutput = implementation.DoToString(in lastInput);
		}
		return lastOutput;
	}

	public static MemoizedToString<TInput, TToString> GetNew()
	{
		return defaultValue;
	}

	static MemoizedToString()
	{
		TInput input = default(TInput);
		TToString val = default(TToString);
		defaultValue = new MemoizedToString<TInput, TToString>
		{
			lastInput = input,
			lastOutput = val.DoToString(in input),
			implementation = val
		};
	}
}
