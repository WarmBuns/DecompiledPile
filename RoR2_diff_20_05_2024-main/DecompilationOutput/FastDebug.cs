using System;
using System.Diagnostics;
using RoR2;
using UnityEngine;

public static class FastDebug
{
	[Conditional("YEET")]
	public static void Log(object message, UnityEngine.Object context)
	{
		UnityEngine.Debug.Log(message, context);
	}

	[Conditional("YEET")]
	public static void Log(object message)
	{
		UnityEngine.Debug.Log(message);
	}

	[Conditional("YEET")]
	public static void LogFormat(string message, params object[] args)
	{
		UnityEngine.Debug.LogFormat(message, args);
	}

	[Conditional("YEET")]
	public static void LogFormat(UnityEngine.Object context, string message, params object[] args)
	{
		UnityEngine.Debug.LogFormat(context, message, args);
	}

	[Conditional("YEET")]
	public static void LogWarning(object message, UnityEngine.Object context)
	{
		UnityEngine.Debug.LogWarning(message, context);
	}

	[Conditional("YEET")]
	public static void LogWarning(object message)
	{
		UnityEngine.Debug.LogWarning(message);
	}

	[Conditional("YEET")]
	public static void LogWarningFormat(string message, params object[] args)
	{
		UnityEngine.Debug.LogWarningFormat(message, args);
	}

	[Conditional("YEET")]
	public static void LogWarningFormat(UnityEngine.Object context, string message, params object[] args)
	{
		UnityEngine.Debug.LogWarningFormat(context, message, args);
	}

	[Conditional("YEET")]
	public static void LogError(object message, UnityEngine.Object context)
	{
		UnityEngine.Debug.LogError(message, context);
	}

	[Conditional("YEET")]
	public static void LogError(object message)
	{
		UnityEngine.Debug.LogError(message);
	}

	[Conditional("YEET")]
	public static void LogErrorFormat(string message, params object[] args)
	{
		UnityEngine.Debug.LogErrorFormat(message, args);
	}

	[Conditional("YEET")]
	public static void LogErrorFormat(UnityEngine.Object context, string message, params object[] args)
	{
		UnityEngine.Debug.LogErrorFormat(context, message, args);
	}

	[Conditional("YEET")]
	public static void LogAssertion(object message, UnityEngine.Object context)
	{
	}

	[Conditional("YEET")]
	public static void LogAssertion(object message)
	{
	}

	[Conditional("YEET")]
	public static void LogAssertionFormat(string message, params object[] args)
	{
	}

	[Conditional("YEET")]
	public static void LogAssertion(UnityEngine.Object context, string message, params object[] args)
	{
	}

	[Conditional("YEET")]
	public static void LogException(Exception e, UnityEngine.Object context)
	{
		UnityEngine.Debug.LogException(e, context);
	}

	[Conditional("YEET")]
	public static void LogException(Exception e)
	{
		UnityEngine.Debug.LogException(e);
	}

	public static void LogChat(object message)
	{
		Chat.AddMessage(message.ToString());
	}
}
