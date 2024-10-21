using System;
using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public class IntroCutsceneController : MonoBehaviour
{
	private static BoolConVar cvIntroSkip = new BoolConVar("intro_skip", ConVarFlags.Archive, "0", "Whether or not to skip the opening cutscene.");

	private static bool _shouldSkip = false;

	private static bool skipRequested = false;

	public static bool shouldSkip
	{
		get
		{
			if (!_shouldSkip)
			{
				return cvIntroSkip.value;
			}
			return true;
		}
		set
		{
			if (value != _shouldSkip)
			{
				_shouldSkip = value;
				if (_shouldSkip)
				{
					IntroCutsceneController.onShouldSkipEnabled?.Invoke();
				}
			}
		}
	}

	private static event Action onShouldSkipEnabled;

	public void Start()
	{
		RoR2Application.instance.PushThreadPriority(ThreadPriority.Normal);
		onShouldSkipEnabled += delegate
		{
			if (RoR2Application.loadFinished)
			{
				Finish();
			}
			else
			{
				RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, new Action(Finish));
			}
		};
	}

	public void Finish()
	{
		RoR2Application.instance.PopThreadPriority();
		if (!skipRequested)
		{
			if (RoR2Application.loadFinished)
			{
				TransitionToTitle();
			}
			else
			{
				RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, new Action(TransitionToTitle));
			}
		}
	}

	public void TransitionToTitle()
	{
		Console.instance.SubmitCmd(null, "set_scene title");
		skipRequested = true;
	}
}
