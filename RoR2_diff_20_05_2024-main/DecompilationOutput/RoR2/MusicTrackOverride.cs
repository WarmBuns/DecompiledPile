using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class MusicTrackOverride : MonoBehaviour
{
	public MusicTrackDef track;

	public int priority;

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		if (InstanceTracker.GetInstancesList<MusicTrackOverride>().Count == 1)
		{
			MusicController.pickTrackHook += PickMusicTrack;
		}
	}

	private void OnDisable()
	{
		if (InstanceTracker.GetInstancesList<MusicTrackOverride>().Count == 1)
		{
			MusicController.pickTrackHook -= PickMusicTrack;
		}
		InstanceTracker.Remove(this);
	}

	private static void PickMusicTrack(MusicController musicController, ref MusicTrackDef newTrack)
	{
		List<MusicTrackOverride> instancesList = InstanceTracker.GetInstancesList<MusicTrackOverride>();
		int num = int.MinValue;
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			MusicTrackOverride musicTrackOverride = instancesList[i];
			int num2 = musicTrackOverride.priority;
			if (num < num2)
			{
				num = num2;
				newTrack = musicTrackOverride.track;
			}
		}
	}
}
