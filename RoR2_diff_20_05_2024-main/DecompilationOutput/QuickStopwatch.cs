using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class QuickStopwatch
{
	private static string m_name;

	private static Stopwatch m_stopwatch = new Stopwatch();

	private static long m_lapTime = 0L;

	private static int m_lap = 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Begin(string name)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void End()
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Lap(string addText = "")
	{
	}
}
