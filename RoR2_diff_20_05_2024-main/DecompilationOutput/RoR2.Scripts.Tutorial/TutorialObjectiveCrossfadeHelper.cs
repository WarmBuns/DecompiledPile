using System;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.Scripts.Tutorial;

public class TutorialObjectiveCrossfadeHelper : MonoBehaviour
{
	public UnityEvent OnBeginFinishCrossfade;

	public Action FinishCrossfadeAction { get; set; }

	public void FinishCrossfade()
	{
		FinishCrossfadeAction?.Invoke();
		OnBeginFinishCrossfade?.Invoke();
	}
}
