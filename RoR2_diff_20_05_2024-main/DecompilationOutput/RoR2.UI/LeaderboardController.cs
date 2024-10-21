using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2.UI;

public class LeaderboardController : MonoBehaviour
{
	public GameObject stripPrefab;

	public RectTransform container;

	public GameObject noEntryObject;

	public AnimateImageAlpha animateImageAlpha;

	public RequestType currentRequestType;

	public int entriesPerPage = 16;

	public MPButton nextPageButton;

	public MPButton previousPageButton;

	public MPButton resetPageButton;

	private WeeklyRunScreenController weeklyRunScreenController;

	private LeaderboardManager _currentLeaderboardManager;

	private List<LeaderboardStrip> stripList = new List<LeaderboardStrip>();

	private List<LeaderboardInfo> leaderboardInfoList = new List<LeaderboardInfo>();

	private bool isRequestQueued;

	public bool IsValid => CurrentLeaderboardManager?.IsValid ?? false;

	public bool IsQuerying => CurrentLeaderboardManager?.IsQuerying ?? false;

	public LeaderboardManager CurrentLeaderboardManager
	{
		get
		{
			if (_currentLeaderboardManager == null)
			{
				_currentLeaderboardManager = new SteamLeaderboardManager(this);
			}
			return _currentLeaderboardManager;
		}
	}

	public string currentLeaderboardName { get; private set; }

	public int currentPage { get; private set; }

	private void Update()
	{
		if (CurrentLeaderboardManager != null && CurrentLeaderboardManager.IsValid && !CurrentLeaderboardManager.IsQuerying && isRequestQueued)
		{
			leaderboardInfoList = CurrentLeaderboardManager.GetLeaderboardInfoList();
			Rebuild();
			isRequestQueued = false;
		}
		if ((bool)noEntryObject)
		{
			noEntryObject.SetActive(leaderboardInfoList.Count == 0);
		}
	}

	private void SetStripCount(int newCount)
	{
		while (stripList.Count > newCount)
		{
			UnityEngine.Object.Destroy(stripList[stripList.Count - 1].gameObject);
			stripList.RemoveAt(stripList.Count - 1);
		}
		while (stripList.Count < newCount)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(stripPrefab, container);
			stripList.Add(gameObject.GetComponent<LeaderboardStrip>());
		}
	}

	private void Rebuild()
	{
		int num = Mathf.Min(leaderboardInfoList.Count, entriesPerPage);
		SetStripCount(entriesPerPage);
		for (int i = 0; i < num; i++)
		{
			LeaderboardInfo leaderboardInfo = leaderboardInfoList[i];
			int num2 = Mathf.FloorToInt(leaderboardInfo.timeInSeconds / 60f);
			float num3 = leaderboardInfo.timeInSeconds - (float)(num2 * 60);
			string text = $"{num2:0}:{num3:00.00}";
			LeaderboardStrip leaderboardStrip = stripList[i];
			leaderboardStrip.rankLabel.text = leaderboardInfo.rank.ToString();
			leaderboardStrip.usernameLabel.userId = CurrentLeaderboardManager.GetUserID(leaderboardInfo);
			leaderboardStrip.timeLabel.text = text;
			leaderboardStrip.classIcon.enabled = true;
			if (leaderboardInfo.survivorIndex.HasValue)
			{
				leaderboardStrip.classIcon.texture = SurvivorCatalog.GetSurvivorPortrait(leaderboardInfo.survivorIndex.Value);
			}
			else
			{
				leaderboardStrip.classIcon.enabled = false;
			}
			leaderboardStrip.isMeImage.enabled = string.CompareOrdinal(leaderboardInfo.userID, CurrentLeaderboardManager.GetLocalUserIdString()) == 0;
			leaderboardStrip.usernameLabel.Refresh();
		}
		for (int j = num; j < entriesPerPage; j++)
		{
			stripList[j].rankLabel.text = "";
			stripList[j].usernameLabel.userId = new PlatformID(0uL);
			stripList[j].timeLabel.text = "";
			stripList[j].classIcon.enabled = false;
			stripList[j].isMeImage.enabled = false;
			stripList[j].usernameLabel.Refresh();
		}
	}

	public void SetRequestedInfo(string newLeaderboardName, RequestType newRequestType, int newPage)
	{
		bool flag = currentLeaderboardName != newLeaderboardName;
		if (flag)
		{
			currentLeaderboardName = newLeaderboardName;
			CurrentLeaderboardManager.UpdateLeaderboard();
			newPage = 0;
		}
		bool flag2 = currentRequestType != newRequestType || flag;
		bool flag3 = currentPage != newPage || flag;
		if (flag2)
		{
			currentRequestType = newRequestType;
		}
		if (flag3)
		{
			currentPage = newPage;
		}
		isRequestQueued = flag || flag2 || flag3;
	}

	private void GenerateFakeLeaderboardList(int count)
	{
		leaderboardInfoList.Clear();
		for (int i = 1; i <= count; i++)
		{
			LeaderboardInfo item = default(LeaderboardInfo);
			item.userID = "76561197995890564";
			item.survivorIndex = (SurvivorIndex)UnityEngine.Random.Range(0, SurvivorCatalog.survivorCount - 1);
			item.timeInSeconds = UnityEngine.Random.Range(120f, 600f);
			leaderboardInfoList.Add(item);
		}
	}

	public void SetRequestType(string requestTypeName)
	{
		if (Enum.TryParse<RequestType>(requestTypeName, ignoreCase: false, out var result))
		{
			currentRequestType = result;
		}
	}

	private void SetRequestedInfoAsync(RequestType type)
	{
	}

	public void SetRequestedInfoProcessAsync(LeaderboardInfo[] leaderboardScores)
	{
	}

	private void OrderLeaderboardListByTime(ref List<LeaderboardInfo> leaderboardInfoList)
	{
		leaderboardInfoList.Sort(SortByTime);
	}

	private static int SortByTime(LeaderboardInfo p1, LeaderboardInfo p2)
	{
		return p1.timeInSeconds.CompareTo(p2.timeInSeconds);
	}
}
