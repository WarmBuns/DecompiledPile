using System;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PauseTintController : MonoBehaviour
{
	private PostProcessVolume ppPauseVolume;

	private void Awake()
	{
		ppPauseVolume = GetComponent<PostProcessVolume>();
		ppPauseVolume.enabled = false;
	}

	private void OnEnable()
	{
		PauseManager.onPauseStartGlobal = (Action)Delegate.Combine(PauseManager.onPauseStartGlobal, new Action(OnPauseStart));
		PauseManager.onPauseEndGlobal = (Action)Delegate.Combine(PauseManager.onPauseEndGlobal, new Action(OnPauseEnd));
	}

	private void OnDisable()
	{
		PauseManager.onPauseStartGlobal = (Action)Delegate.Remove(PauseManager.onPauseStartGlobal, new Action(OnPauseStart));
		PauseManager.onPauseEndGlobal = (Action)Delegate.Remove(PauseManager.onPauseEndGlobal, new Action(OnPauseEnd));
	}

	private void OnPauseStart()
	{
		ppPauseVolume.enabled = true;
	}

	private void OnPauseEnd()
	{
		ppPauseVolume.enabled = false;
	}
}
