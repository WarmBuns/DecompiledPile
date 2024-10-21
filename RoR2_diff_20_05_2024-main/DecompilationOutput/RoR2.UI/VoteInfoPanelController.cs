using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class VoteInfoPanelController : MonoBehaviour
{
	private struct IndicatorInfo
	{
		public GameObject gameObject;

		public Image image;

		public TooltipProvider tooltipProvider;
	}

	public GameObject indicatorPrefab;

	public Sprite hasNotVotedSprite;

	public Sprite hasVotedSprite;

	public RectTransform container;

	public GameObject timerPanelObject;

	public TextMeshProUGUI timerLabel;

	public VoteController voteController;

	private readonly List<IndicatorInfo> indicators = new List<IndicatorInfo>();

	private bool votesArePossible => RoR2Application.isInMultiPlayer;

	private void Awake()
	{
		if (!votesArePossible)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void AllocateIndicators(int desiredIndicatorCount)
	{
		while (indicators.Count > desiredIndicatorCount)
		{
			int index = indicators.Count - 1;
			Object.Destroy(indicators[index].gameObject);
			indicators.RemoveAt(index);
		}
		while (indicators.Count < desiredIndicatorCount)
		{
			GameObject gameObject = Object.Instantiate(indicatorPrefab, container);
			gameObject.SetActive(value: true);
			indicators.Add(new IndicatorInfo
			{
				gameObject = gameObject,
				image = gameObject.GetComponentInChildren<Image>(),
				tooltipProvider = gameObject.GetComponentInChildren<TooltipProvider>()
			});
		}
		timerPanelObject.transform.SetAsLastSibling();
	}

	public void UpdateElements()
	{
		int num = 0;
		if ((bool)voteController)
		{
			num = voteController.GetVoteCount();
		}
		AllocateIndicators(num);
		for (int i = 0; i < num; i++)
		{
			UserVote vote = voteController.GetVote(i);
			indicators[i].image.sprite = (vote.receivedVote ? hasVotedSprite : hasNotVotedSprite);
			string userName;
			if ((bool)vote.networkUserObject)
			{
				NetworkUser component = vote.networkUserObject.GetComponent<NetworkUser>();
				userName = ((!component) ? Language.GetString("PLAYER_NAME_UNAVAILABLE") : component.GetNetworkPlayerName().GetResolvedName());
			}
			else
			{
				userName = Language.GetString("PLAYER_NAME_DISCONNECTED");
			}
			indicators[i].tooltipProvider.SetContent(TooltipProvider.GetPlayerNameTooltipContent(userName));
		}
		bool flag = (bool)voteController && voteController.timerStartCondition != VoteController.TimerStartCondition.Never && !float.IsInfinity(voteController.timer);
		timerPanelObject.SetActive(flag);
		if (flag)
		{
			float num2 = voteController.timer;
			if (num2 < 0f)
			{
				num2 = 0f;
			}
			int num3 = Mathf.FloorToInt(num2 * (1f / 60f));
			int num4 = (int)num2 - num3 * 60;
			timerLabel.text = $"{num3}:{num4:00}";
		}
	}

	private void Update()
	{
		UpdateElements();
	}
}
