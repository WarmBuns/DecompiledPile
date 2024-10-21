using System;
using JetBrains.Annotations;

namespace RoR2;

public class CallbackCheck<TResult, TArg> where TResult : struct
{
	public delegate void CallbackDelegate(TArg arg, ref TResult? resultOverride);

	private CallbackDelegate[] callbacks = Array.Empty<CallbackDelegate>();

	private int callbackCount;

	public void AddCallback([NotNull] CallbackDelegate callback)
	{
		if (callbacks.Length <= callbackCount + 1)
		{
			Array.Resize(ref callbacks, callbackCount + 1);
		}
		callbacks[callbackCount++] = callback;
	}

	public void RemoveCallback([NotNull] CallbackDelegate callback)
	{
		for (int i = 0; i < callbackCount; i++)
		{
			if ((object)callbacks[i] == callback)
			{
				for (int num = callbackCount - 1; i < num; i++)
				{
					callbacks[i] = callbacks[i + 1];
				}
				callbacks[--callbackCount] = null;
				break;
			}
		}
	}

	public void Clear()
	{
		Array.Clear(callbacks, 0, callbackCount);
		callbackCount = 0;
	}

	public TResult? Evaluate(TArg arg)
	{
		TResult? resultOverride = null;
		for (int i = 0; i < callbackCount; i++)
		{
			callbacks[i](arg, ref resultOverride);
			if (resultOverride.HasValue)
			{
				break;
			}
		}
		return resultOverride;
	}
}
