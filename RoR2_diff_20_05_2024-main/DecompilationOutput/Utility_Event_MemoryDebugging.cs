using System;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

public class Utility_Event_MemoryDebugging : MonoBehaviour
{
	[Flags]
	public enum EventFlags
	{
		None = 0,
		FixedUpdate = 2,
		EarlyUpdate = 4,
		Update = 8,
		LateUpdate = 0x10,
		Render = 0x20
	}

	public enum MemoryType
	{
		MonoUsed,
		TotalAllocated,
		FragmentationInfo
	}

	public const int arraySize = 10000;

	private static bool staticRecordData;

	private static EventFlags staticEventsToTrack;

	private static MemoryType staticMemoryTrackType;

	private static int index = 0;

	private static int lastRecordedRenderIndex = -1;

	private static long[] monoUsedEarlyUpdate = new long[10000];

	private static long[] monoUsedUpdate = new long[10000];

	private static long[] monoUsedLateUpdate = new long[10000];

	private static long[] monoUsedFixedUpdate = new long[10000];

	private static long[] monoUsedRender = new long[10000];

	public EventFlags EventsToTrack;

	private EventFlags prevEventsToTrack;

	[Header("Should we record this info")]
	public bool RecordData;

	private bool prevRecordData;

	[Header("What type of memory should we be tracking?")]
	public MemoryType memoryTrackType;

	private MemoryType prevMemoryTrackType;

	[Header("In case you need to print it again")]
	public bool DumpDebugDataToConsole;

	[Header("The most memory gained so far")]
	public long mostMemoryIncrease = long.MinValue;

	public EventFlags mostMemoryIncreaseEvent;

	[Header("For viewing only - editing does nothing")]
	[Space(20f)]
	public string Index;

	public bool IncreasingBetweenUpdate;

	public bool IncreasingBetweenEarlyUpdate;

	public bool IncreasingBetweenLateUpdate;

	public bool IncreasingBetweenFixedUpdate;

	private long previousLong;

	private long temporaryLong;

	private long previousLongInThisArray;

	protected bool amIPreDefaultTime;

	private NativeArray<int> fragmentationInfo;

	private void FixedUpdate()
	{
		if (DumpDebugDataToConsole && index > 0)
		{
			PrintAllDataRecorded();
			DumpDebugDataToConsole = false;
		}
		if (ShouldWeRecordAnything() && AreWeTrackingThisEvent(EventFlags.FixedUpdate | EventFlags.Render))
		{
			RecordCurrentMemory(ref monoUsedFixedUpdate, ref IncreasingBetweenFixedUpdate, EventFlags.FixedUpdate);
		}
	}

	private void EarlyUpdate()
	{
		if (ShouldWeRecordAnything() && AreWeTrackingThisEvent(EventFlags.EarlyUpdate))
		{
			RecordCurrentMemory(ref monoUsedEarlyUpdate, ref IncreasingBetweenEarlyUpdate, EventFlags.EarlyUpdate);
		}
	}

	private void Update()
	{
		if (ShouldWeRecordAnything() && AreWeTrackingThisEvent(EventFlags.Update))
		{
			RecordCurrentMemory(ref monoUsedUpdate, ref IncreasingBetweenUpdate, EventFlags.Update);
		}
	}

	private void LateUpdate()
	{
		if (ShouldWeRecordAnything() && (AreWeTrackingThisEvent(EventFlags.LateUpdate) || AreWeTrackingThisEvent(EventFlags.Render)))
		{
			RecordCurrentMemory(ref monoUsedLateUpdate, ref IncreasingBetweenLateUpdate, EventFlags.LateUpdate);
			TryIncrementIndex();
		}
	}

	private bool AreWeTrackingThisEvent(EventFlags eventFlag)
	{
		if (EventsToTrack != prevEventsToTrack)
		{
			staticEventsToTrack = EventsToTrack;
			prevEventsToTrack = EventsToTrack;
		}
		if (EventsToTrack != staticEventsToTrack)
		{
			EventsToTrack = staticEventsToTrack;
			prevEventsToTrack = staticEventsToTrack;
		}
		return staticEventsToTrack.HasFlag(eventFlag);
	}

	private bool ShouldWeRecordAnything()
	{
		if (prevMemoryTrackType != memoryTrackType)
		{
			prevMemoryTrackType = memoryTrackType;
			staticMemoryTrackType = memoryTrackType;
			if (staticRecordData)
			{
				ResetAllData();
			}
		}
		if (prevRecordData != RecordData)
		{
			prevRecordData = RecordData;
			staticRecordData = RecordData;
			if (!RecordData)
			{
				PrintAllDataRecorded();
			}
			else
			{
				ResetAllData();
			}
		}
		if (staticRecordData != RecordData)
		{
			RecordData = staticRecordData;
			prevRecordData = staticRecordData;
		}
		return staticRecordData;
	}

	private void TryIncrementIndex()
	{
		float num = index / 10000;
		Index = (float)(int)(num * 100f) / 100f + "% Complete";
		if (!amIPreDefaultTime)
		{
			if (index < 9998)
			{
				index++;
			}
			else if (staticRecordData)
			{
				staticRecordData = false;
				PrintAllDataRecorded();
			}
		}
	}

	private long GetMemoryValue()
	{
		return staticMemoryTrackType switch
		{
			MemoryType.TotalAllocated => Profiler.GetTotalAllocatedMemoryLong(), 
			MemoryType.FragmentationInfo => Profiler.GetTotalFragmentationInfo(fragmentationInfo), 
			_ => Profiler.GetMonoUsedSizeLong(), 
		};
	}

	private void RecordCurrentMemory(ref long[] _array, ref bool increasing, EventFlags _event)
	{
		temporaryLong = GetMemoryValue();
		if (staticEventsToTrack.HasFlag(EventFlags.Render))
		{
			if (!amIPreDefaultTime && _event == EventFlags.LateUpdate)
			{
				monoUsedRender[index] = temporaryLong;
			}
			else if (amIPreDefaultTime && _event == EventFlags.FixedUpdate && lastRecordedRenderIndex != index)
			{
				monoUsedRender[index] = temporaryLong - monoUsedRender[index];
				lastRecordedRenderIndex = index;
			}
		}
		if (!amIPreDefaultTime)
		{
			temporaryLong -= _array[index];
			RecordMostMemoryGain(temporaryLong - previousLong, _event);
			previousLong = temporaryLong;
			increasing = temporaryLong > 0;
		}
		_array[index] = temporaryLong;
	}

	private void TryRecordRenderMemoryGain()
	{
		if (amIPreDefaultTime && index >= 1 && index != lastRecordedRenderIndex)
		{
			lastRecordedRenderIndex = index;
			monoUsedRender[index] = monoUsedFixedUpdate[index] - monoUsedLateUpdate[index - 1];
		}
	}

	private void RecordMostMemoryGain(long amount, EventFlags _event)
	{
		if (mostMemoryIncrease < amount)
		{
			mostMemoryIncrease = amount;
			mostMemoryIncreaseEvent = _event;
		}
	}

	private void ResetAllData()
	{
		index = 0;
		monoUsedEarlyUpdate = new long[10000];
		monoUsedUpdate = new long[10000];
		monoUsedLateUpdate = new long[10000];
		monoUsedFixedUpdate = new long[10000];
		monoUsedRender = new long[10000];
	}

	private void PrintAllDataRecorded()
	{
		StringBuilder sb2 = new StringBuilder();
		sb2.Append($"<color=green>Memory Debugging Data results</color> -  Tracked Memory:  {staticMemoryTrackType}");
		sb2.Append($"\r\nMost Memory in a single step: {mostMemoryIncrease} ({mostMemoryIncreaseEvent})");
		long averageMemoryChangePerStep;
		long totalMemoryChange;
		if (staticEventsToTrack.HasFlag(EventFlags.FixedUpdate))
		{
			CalculateDataFromArray(ref sb2, EventFlags.FixedUpdate, ref monoUsedFixedUpdate);
		}
		if (staticEventsToTrack.HasFlag(EventFlags.EarlyUpdate))
		{
			CalculateDataFromArray(ref sb2, EventFlags.EarlyUpdate, ref monoUsedEarlyUpdate);
		}
		if (staticEventsToTrack.HasFlag(EventFlags.Update))
		{
			CalculateDataFromArray(ref sb2, EventFlags.Update, ref monoUsedUpdate);
		}
		if (staticEventsToTrack.HasFlag(EventFlags.LateUpdate))
		{
			CalculateDataFromArray(ref sb2, EventFlags.LateUpdate, ref monoUsedLateUpdate);
		}
		if (staticEventsToTrack.HasFlag(EventFlags.Render))
		{
			CalculateDataFromArray(ref sb2, EventFlags.Render, ref monoUsedRender);
		}
		totalMemoryChange = 0L;
		void CalculateDataFromArray(ref StringBuilder sb, EventFlags _event, ref long[] _array)
		{
			averageMemoryChangePerStep = 0L;
			totalMemoryChange = 0L;
			for (int i = 0; i < index; i++)
			{
				long num = _array[i];
				if (i != 0)
				{
					totalMemoryChange += num;
				}
			}
			averageMemoryChangePerStep = totalMemoryChange / Mathf.Max(index, 1);
			sb.Append($"\r\n\r\n<bold>{_event}</bold>: Avg change per step {FormatLongAsText(averageMemoryChangePerStep)}. | Total Memory changed: {FormatLongAsText(totalMemoryChange)}");
		}
		static string FormatLongAsText(long value)
		{
			if (value > 1000000000)
			{
				return $"<color=red>{(float)value / 1E+09f}GB </color> {value}";
			}
			if (value > 1000000)
			{
				return $"<color=orange>{(float)value / 1000000f}MB </color> {value}";
			}
			return $"{(float)value / 1000f}KB ({value})";
		}
	}
}
