using System;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class WeeklyRunScreenController : MonoBehaviour
{
	public LeaderboardController leaderboard;

	public TextMeshProUGUI countdownLabel;

	private uint currentCycle;

	private TimeSpan lastCountdown;

	private float labelFadeValue;

	private bool leaderboardInitiated;

	private void OnEnable()
	{
		currentCycle = WeeklyRun.GetCurrentSeedCycle();
		UpdateLeaderboard();
	}

	private void UpdateLeaderboard()
	{
		leaderboardInitiated = true;
	}

	private void Update()
	{
		uint currentSeedCycle = WeeklyRun.GetCurrentSeedCycle();
		if (currentSeedCycle != currentCycle)
		{
			currentCycle = currentSeedCycle;
			UpdateLeaderboard();
		}
		if (!leaderboardInitiated)
		{
			UpdateLeaderboard();
		}
		TimeSpan timeSpan = WeeklyRun.GetSeedCycleStartDateTime(currentCycle + 1) - WeeklyRun.now;
		string @string = Language.GetString("WEEKLY_RUN_NEXT_CYCLE_COUNTDOWN_FORMAT");
		countdownLabel.text = string.Format(@string, timeSpan.Hours + timeSpan.Days * 24, timeSpan.Minutes, timeSpan.Seconds);
		if (timeSpan != lastCountdown)
		{
			lastCountdown = timeSpan;
			labelFadeValue = 0f;
		}
		labelFadeValue = Mathf.Max(labelFadeValue + Time.deltaTime * 1f, 0f);
		Color white = Color.white;
		if (timeSpan.Days == 0 && timeSpan.Hours == 0)
		{
			white.g = labelFadeValue;
			white.b = labelFadeValue;
		}
		countdownLabel.color = white;
	}
}
