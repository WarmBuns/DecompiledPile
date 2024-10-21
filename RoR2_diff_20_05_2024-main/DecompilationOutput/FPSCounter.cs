using System;
using RoR2;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
	private TextMeshProUGUI unityText;

	private TextMeshProUGUI unityVarText;

	private TextMeshProUGUI cpuText;

	private TextMeshProUGUI gpuText;

	private TextMeshProUGUI memText;

	private static float[] unitySamples;

	private static float[] cpuSamples;

	private static float[] gpuSamples;

	public static int numSamples = 30;

	private static int sampleIndex = 0;

	private int frameSampleIndex;

	public GameObject rootObject;

	public float timingCaptureFrequency = 2f;

	private float captureTimer;

	private char[] tempArray = new char[64];

	private uint numTimings = 5u;

	private FrameTiming[] outTimings;

	private long prevAllocatedBytes;

	private static int waitTurn = 0;

	private static int waitIndex = 0;

	private static int maxWaitIndex = 5;

	public static float currentFPS = 30f;

	private void Start()
	{
		RoR2Application.onUpdate += UpdateFPSLimitVars;
		unitySamples = new float[numSamples];
		base.gameObject.SetActive(value: false);
	}

	private int PutValue(int value, char[] array, int startIndex, bool addPlusIfPositive = false, bool disableMinusIfNegative = false)
	{
		if (value >= 0)
		{
			if (addPlusIfPositive)
			{
				array[startIndex] = '+';
				startIndex++;
			}
		}
		else if (!disableMinusIfNegative)
		{
			value = -value;
			array[startIndex] = '-';
			startIndex++;
		}
		int num = 10;
		int num2 = 1;
		while (num <= value && num != 0)
		{
			num *= 10;
			num2++;
		}
		num /= 10;
		if (num == 0)
		{
			num = 1;
		}
		for (int i = 0; i < num2; i++)
		{
			int digit = GetDigit(value, num);
			num /= 10;
			array[startIndex + i] = (char)(digit + 48);
		}
		return startIndex + num2;
	}

	private int PutValue(int value, char[] array, int startIndex, int numDigits)
	{
		int num = 10;
		int num2 = 1;
		while (num <= value)
		{
			num *= 10;
			num2++;
		}
		num /= 10;
		if (num2 < numDigits)
		{
			for (int i = num2; i < numDigits; i++)
			{
				array[startIndex++] = '0';
			}
		}
		for (int j = 0; j < num2; j++)
		{
			int digit = GetDigit(value, num);
			num /= 10;
			array[startIndex + j] = (char)(digit + 48);
		}
		return startIndex + num2;
	}

	private int PutValue(double value, char[] array, int startIndex, int numDecimalPlaces = 0, bool addPlusIfPositive = false)
	{
		int value2 = (int)value;
		startIndex = PutValue(value2, array, startIndex, addPlusIfPositive);
		if (numDecimalPlaces == 0)
		{
			return startIndex;
		}
		array[startIndex++] = '.';
		double num = 10.0;
		for (int i = 1; i < numDecimalPlaces; i++)
		{
			num *= 10.0;
		}
		double num2 = Math.Abs(value);
		int value3 = (int)((num2 - Math.Floor(num2)) * num);
		startIndex = PutValue(value3, array, startIndex, numDecimalPlaces);
		return startIndex;
	}

	private int PutValue(float value, char[] array, int startIndex, int numDecimalPlaces = 0, bool addPlusIfPositive = false)
	{
		int value2 = (int)value;
		startIndex = PutValue(value2, array, startIndex, addPlusIfPositive);
		if (numDecimalPlaces == 0)
		{
			return startIndex;
		}
		array[startIndex++] = '.';
		float num = 10f;
		for (int i = 1; i < numDecimalPlaces; i++)
		{
			num *= 10f;
		}
		float num2 = Mathf.Abs(value);
		int value3 = (int)((num2 - Mathf.Floor(num2)) * num);
		startIndex = PutValue(value3, array, startIndex, numDecimalPlaces);
		return startIndex;
	}

	private int PutValue(string value, char[] array, int startIndex)
	{
		for (int i = 0; i < value.Length; i++)
		{
			array[startIndex + i] = value[i];
		}
		return startIndex + value.Length;
	}

	private int GetDigit(int value, int place)
	{
		if (place == 1)
		{
			return value % 10;
		}
		int num = place * 10;
		int num2 = value % num;
		int num3 = value % place;
		return (num2 - num3) / place;
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
		if (currentFPS < 28f && waitIndex != waitTurn)
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
		num /= (float)numSamples;
		currentFPS = num;
	}

	private void Update()
	{
	}
}
