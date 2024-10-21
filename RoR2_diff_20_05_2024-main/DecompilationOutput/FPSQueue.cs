using RoR2;
using UnityEngine;

public class FPSQueue : MonoBehaviour
{
	public static float fpsThrottlingCutoff = 28f;

	public static float currentFPS = 30f;

	public static int numSamples = 30;

	private static int sampleIndex = 0;

	private static float[] unitySamples;

	private static int waitTurn = 0;

	private static int waitIndex = 0;

	private static int maxWaitIndex = 5;

	private void Start()
	{
		RoR2Application.onUpdate += UpdateFPSLimitVars;
		unitySamples = new float[numSamples];
	}

	public static int GetWaitIndex()
	{
		waitIndex++;
		if (waitIndex > maxWaitIndex)
		{
			waitIndex = 0;
		}
		return waitIndex;
	}

	public static bool CheckFPSQueue(ref int waitIndex)
	{
		if (waitIndex == 0)
		{
			waitIndex = GetWaitIndex();
		}
		if (currentFPS < fpsThrottlingCutoff && waitIndex != waitTurn)
		{
			return false;
		}
		return true;
	}

	private static void UpdateFPSLimitVars()
	{
		waitTurn++;
		if (waitTurn > maxWaitIndex)
		{
			waitTurn = 0;
		}
		CalculateFramerate();
	}

	private static void CalculateFramerate()
	{
		unitySamples[sampleIndex++] = 1f / Time.unscaledDeltaTime;
		if (sampleIndex == numSamples)
		{
			sampleIndex = 0;
		}
		float num = 0f;
		for (int i = 0; i < numSamples; i++)
		{
			num += unitySamples[i];
		}
		currentFPS = num / (float)numSamples;
	}
}
