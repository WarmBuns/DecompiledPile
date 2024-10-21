using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.RaidGauntletEncounter;

public class Listening : EntityState
{
	public static float delayBeforeBeginningEncounter;

	public static int shardsDestroyedToTriggerEncounter = 5;

	private GameObject exitPortal;

	private List<GameObject> shardList = new List<GameObject>();

	private const float delayBeforeRegisteringShards = 2f;

	private bool hasRegisteredShards;

	private int previousDestroyedShardCount;

	private bool beginEncounterCountdown;

	private bool beginGauntletCountdown;

	private bool gauntletTwentySecondWarning;

	private bool gauntletTenSecondWarning;

	private bool gauntletFiveSecondWarning;

	private bool beginGauntletFinalCountdown;

	private const float delayBeforeBeginningGauntletCountdown = 15f;

	private float gauntletFinalCountdown;

	private int secondsRemaining = 4;

	private bool gauntletEnd;

	private bool hasExitPortal;

	private int totalSeconds;

	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!NetworkServer.active)
		{
			return;
		}
		if (base.fixedAge >= 2f)
		{
			RegisterShards();
			RegisterExitPortal();
		}
		if (base.fixedAge >= 15f && !beginGauntletCountdown)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "GAUNTLET_START"
			});
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "GAUNTLET_FIVE_SHARDS_REMAINING"
			});
			beginGauntletCountdown = true;
		}
		if (base.fixedAge >= 25f && !gauntletTwentySecondWarning)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "GAUNTLET_TWENTY_SECONDS_WARNING"
			});
			gauntletTwentySecondWarning = true;
		}
		if (base.fixedAge >= 35f && !gauntletTenSecondWarning)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "GAUNTLET_TEN_SECONDS_WARNING"
			});
			gauntletTenSecondWarning = true;
		}
		if (base.fixedAge >= 40f && !gauntletFiveSecondWarning)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "GAUNTLET_FIVE_SECONDS_WARNING"
			});
			gauntletFiveSecondWarning = true;
			beginGauntletFinalCountdown = true;
		}
		if (!hasRegisteredShards)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < shardList.Count; i++)
		{
			if (shardList[i] == null)
			{
				num++;
			}
		}
		if (previousDestroyedShardCount != num)
		{
			switch (shardsDestroyedToTriggerEncounter - num)
			{
			case 4:
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = "GAUNTLET_FOUR_SHARDS_REMAINING"
				});
				break;
			case 3:
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = "GAUNTLET_THREE_SHARDS_REMAINING"
				});
				break;
			case 2:
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = "GAUNTLET_TWO_SHARDS_REMAINING"
				});
				break;
			}
		}
		int num2 = shardsDestroyedToTriggerEncounter - 1;
		if (previousDestroyedShardCount < num2 && num >= num2)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "GAUNTLET_ONE_SHARD_REMAINING"
			});
		}
		if (num >= shardsDestroyedToTriggerEncounter && !beginEncounterCountdown)
		{
			beginEncounterCountdown = true;
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "GAUNTLET_ALL_SHARDS_DESTROYED"
			});
		}
		if (beginGauntletFinalCountdown && !gauntletEnd)
		{
			gauntletFinalCountdown += GetDeltaTime();
			if (gauntletFinalCountdown >= 1f)
			{
				secondsRemaining--;
				totalSeconds++;
				if (totalSeconds == 1)
				{
					Chat.SendBroadcastChat(new Chat.SimpleChatMessage
					{
						baseToken = "GAUNTLET_FOUR_SECONDS_REMAINING"
					});
				}
				else if (totalSeconds == 2)
				{
					Chat.SendBroadcastChat(new Chat.SimpleChatMessage
					{
						baseToken = "GAUNTLET_THREE_SECONDS_REMAINING"
					});
				}
				else if (totalSeconds == 3)
				{
					Chat.SendBroadcastChat(new Chat.SimpleChatMessage
					{
						baseToken = "GAUNTLET_TWO_SECONDS_REMAINING"
					});
				}
				else if (totalSeconds == 4)
				{
					Chat.SendBroadcastChat(new Chat.SimpleChatMessage
					{
						baseToken = "GAUNTLET_ONE_SECOND_REMAINING"
					});
				}
				gauntletFinalCountdown = 0f;
			}
			if (secondsRemaining == 0)
			{
				gauntletEnd = true;
				exitPortal.SetActive(value: false);
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = "GAUNTLET_END"
				});
			}
		}
		previousDestroyedShardCount = num;
	}

	private void RegisterShards()
	{
		if (hasRegisteredShards)
		{
			return;
		}
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			if (readOnlyInstancesList[i].name.Contains("GauntletShard"))
			{
				shardList.Add(readOnlyInstancesList[i].gameObject);
			}
		}
		hasRegisteredShards = true;
	}

	private void RegisterExitPortal()
	{
		if (!hasExitPortal)
		{
			exitPortal = GameObject.Find("PortalArena");
			if ((bool)exitPortal)
			{
				hasExitPortal = true;
			}
		}
	}
}
