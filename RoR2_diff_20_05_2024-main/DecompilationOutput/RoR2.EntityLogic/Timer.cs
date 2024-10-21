using System;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.EntityLogic;

public class Timer : MonoBehaviour
{
	public enum TimeStepType
	{
		Time,
		UnscaledTime,
		FixedTime
	}

	[SerializeField]
	private float _duration;

	[SerializeField]
	private TimeStepType _timeStepType = TimeStepType.FixedTime;

	public bool resetTimerOnEnable = true;

	public bool playTimerOnEnable = true;

	public bool loop;

	public UnityEvent action;

	private float stopwatch;

	private bool timerRunning;

	public float duration
	{
		get
		{
			return _duration;
		}
		set
		{
			_duration = value;
		}
	}

	public TimeStepType timeStepType
	{
		get
		{
			return _timeStepType;
		}
		set
		{
			_timeStepType = value;
		}
	}

	private void OnEnable()
	{
		if (resetTimerOnEnable)
		{
			RewindTimerToBeginning();
		}
		if (playTimerOnEnable)
		{
			PlayTimer();
		}
	}

	private void Update()
	{
		if (timerRunning)
		{
			if (timeStepType == TimeStepType.Time)
			{
				RunTimer(Time.deltaTime);
			}
			else if (timeStepType == TimeStepType.UnscaledTime)
			{
				RunTimer(Time.unscaledDeltaTime);
			}
		}
	}

	private void FixedUpdate()
	{
		if (timerRunning && timeStepType == TimeStepType.FixedTime)
		{
			RunTimer(Time.fixedDeltaTime);
		}
	}

	public void RewindTimerToBeginning()
	{
		stopwatch = 0f;
	}

	public void SkipTimerToEnd()
	{
		stopwatch = duration;
	}

	public void SetTimerPlaying(bool newTimerRunning)
	{
		timerRunning = newTimerRunning;
	}

	public void PlayTimer()
	{
		SetTimerPlaying(newTimerRunning: true);
	}

	public void PauseTimer()
	{
		SetTimerPlaying(newTimerRunning: false);
	}

	public void CancelTimer()
	{
		PauseTimer();
		RewindTimerToBeginning();
	}

	public void PlayTimerFromBeginning()
	{
		RewindTimerToBeginning();
		PlayTimer();
	}

	private void RunTimer(float deltaTime)
	{
		stopwatch += deltaTime;
		if (stopwatch >= duration)
		{
			stopwatch = 0f;
			try
			{
				action?.Invoke();
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			if (!loop)
			{
				timerRunning = false;
			}
		}
	}
}
