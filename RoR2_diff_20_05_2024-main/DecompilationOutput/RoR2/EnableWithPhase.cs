using System;
using UnityEngine;

namespace RoR2;

public class EnableWithPhase : MonoBehaviour
{
	[Serializable]
	public struct PhaseInfo
	{
		public int minimumPhaseThreshold;

		public int maximumPhaseThreshold;

		public GameObject[] gameObjectsToEnable;
	}

	public PhaseInfo[] phaseInfos;

	private void FixedUpdate()
	{
		if (!PhaseCounter.instance)
		{
			return;
		}
		int phase = PhaseCounter.instance.phase;
		for (int i = 0; i < phaseInfos.Length; i++)
		{
			bool active = true;
			PhaseInfo phaseInfo = phaseInfos[i];
			if (phaseInfo.minimumPhaseThreshold > phase || phaseInfo.maximumPhaseThreshold < phase)
			{
				active = false;
			}
			GameObject[] gameObjectsToEnable = phaseInfo.gameObjectsToEnable;
			for (int j = 0; j < gameObjectsToEnable.Length; j++)
			{
				gameObjectsToEnable[j].SetActive(active);
			}
		}
	}
}
