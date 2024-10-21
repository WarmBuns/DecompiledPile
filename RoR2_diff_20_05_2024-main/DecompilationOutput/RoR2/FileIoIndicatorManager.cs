using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2;

public static class FileIoIndicatorManager
{
	private static int activeWriteCount;

	private static volatile float saveIconAlpha;

	public static void IncrementActiveWriteCount()
	{
		Interlocked.Increment(ref activeWriteCount);
		saveIconAlpha = 2f;
	}

	public static void DecrementActiveWriteCount()
	{
		Interlocked.Decrement(ref activeWriteCount);
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		Image saveImage = RoR2Application.instance.saveIconImage;
		RoR2Application.onUpdate += delegate
		{
			Color color = saveImage.color;
			if (activeWriteCount <= 0)
			{
				color.a = (saveIconAlpha = Mathf.Max(saveIconAlpha - 4f * Time.unscaledDeltaTime, 0f));
			}
			saveImage.color = color;
		};
	}
}
