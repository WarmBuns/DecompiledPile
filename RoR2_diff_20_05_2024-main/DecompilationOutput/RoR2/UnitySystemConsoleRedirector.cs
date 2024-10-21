using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RoR2;

internal static class UnitySystemConsoleRedirector
{
	private class OutWriter : UnityTextWriter
	{
		public override void WriteBufferToUnity(string str)
		{
		}
	}

	private class ErrorWriter : UnityTextWriter
	{
		public override void WriteBufferToUnity(string str)
		{
			Debug.LogError(str);
		}
	}

	private abstract class UnityTextWriter : TextWriter
	{
		private StringBuilder buffer = new StringBuilder();

		public override Encoding Encoding => Encoding.Default;

		public override void Flush()
		{
			WriteBufferToUnity(buffer.ToString());
			buffer.Length = 0;
		}

		public abstract void WriteBufferToUnity(string str);

		public override void Write(string value)
		{
			buffer.Append(value);
			if (value != null)
			{
				int length = value.Length;
				if (length > 0 && value[length - 1] == '\n')
				{
					Flush();
				}
			}
		}

		public override void Write(char value)
		{
			buffer.Append(value);
			if (value == '\n')
			{
				Flush();
			}
		}

		public override void Write(char[] value, int index, int count)
		{
			Write(new string(value, index, count));
		}
	}

	private static TextWriter oldOut;

	private static TextWriter oldError;

	public static void Redirect()
	{
		oldOut = System.Console.Out;
		System.Console.SetOut(new OutWriter());
		oldError = System.Console.Error;
		System.Console.SetError(new ErrorWriter());
	}

	public static void Disengage()
	{
		if (oldOut != null)
		{
			System.Console.SetOut(oldOut);
			oldOut = null;
		}
		if (oldError != null)
		{
			System.Console.SetError(oldError);
			oldError = null;
		}
	}
}
