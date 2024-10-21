using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.SuperRoboBallEncounter;

public class Listening : EntityState
{
	public static float delayBeforeBeginningEncounter;

	public static int eggsDestroyedToTriggerEncounter;

	private ScriptedCombatEncounter scriptedCombatEncounter;

	private List<GameObject> eggList = new List<GameObject>();

	private const float delayBeforeRegisteringEggs = 2f;

	private bool hasRegisteredEggs;

	private int previousDestroyedEggCount;

	private bool beginEncounterCountdown;

	private float encounterCountdown;

	public override void OnEnter()
	{
		base.OnEnter();
		scriptedCombatEncounter = GetComponent<ScriptedCombatEncounter>();
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
			RegisterEggs();
		}
		if (!hasRegisteredEggs)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < eggList.Count; i++)
		{
			if (eggList[i] == null)
			{
				num++;
			}
		}
		int num2 = eggsDestroyedToTriggerEncounter - 1;
		if (previousDestroyedEggCount < num2 && num >= num2)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "VULTURE_EGG_WARNING"
			});
		}
		if (num >= eggsDestroyedToTriggerEncounter && !beginEncounterCountdown)
		{
			encounterCountdown = delayBeforeBeginningEncounter;
			beginEncounterCountdown = true;
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "VULTURE_EGG_BEGIN"
			});
		}
		if (beginEncounterCountdown)
		{
			encounterCountdown -= GetDeltaTime();
			if (encounterCountdown <= 0f)
			{
				scriptedCombatEncounter.BeginEncounter();
				outer.SetNextState(new Idle());
			}
		}
		previousDestroyedEggCount = num;
	}

	private void RegisterEggs()
	{
		if (hasRegisteredEggs)
		{
			return;
		}
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			if (readOnlyInstancesList[i].name.Contains("VultureEgg"))
			{
				eggList.Add(readOnlyInstancesList[i].gameObject);
			}
		}
		hasRegisteredEggs = true;
	}
}
