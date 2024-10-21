using System;
using EntityStates.Interactables.GoldBeacon;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.Goldshores;

public class GoldshoresBossfight : EntityState
{
	private GoldshoresMissionController missionController;

	public static float shieldRemovalDuration;

	public static GameObject shieldRemovalEffectPrefab;

	public static GameObject shieldRegenerationEffectPrefab;

	public static GameObject combatEncounterPrefab;

	private static float transitionDuration = 3f;

	private bool hasSpawnedBoss;

	private int serverCycleCount;

	private Run.FixedTimeStamp bossInvulnerabilityStartTime;

	private ScriptedCombatEncounter scriptedCombatEncounter;

	private bool bossImmunity;

	private bool bossShouldBeInvulnerable => missionController.beaconsActive < missionController.beaconsToSpawnOnMap;

	public static event Action onOneCycleGoldTitanKill;

	public override void OnEnter()
	{
		base.OnEnter();
		missionController = GetComponent<GoldshoresMissionController>();
		bossInvulnerabilityStartTime = Run.FixedTimeStamp.negativeInfinity;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			ServerFixedUpdate();
		}
	}

	private void SetBossImmunity(bool newBossImmunity)
	{
		if (!scriptedCombatEncounter || newBossImmunity == bossImmunity)
		{
			return;
		}
		bossImmunity = newBossImmunity;
		foreach (CharacterMaster readOnlyMembers in scriptedCombatEncounter.combatSquad.readOnlyMembersList)
		{
			CharacterBody body = readOnlyMembers.GetBody();
			if ((bool)body)
			{
				if (bossImmunity)
				{
					body.AddBuff(RoR2Content.Buffs.Immune);
					continue;
				}
				EffectManager.SpawnEffect(shieldRemovalEffectPrefab, new EffectData
				{
					origin = body.coreTransform.position
				}, transmit: true);
				body.RemoveBuff(RoR2Content.Buffs.Immune);
			}
		}
	}

	private void ExtinguishBeacons()
	{
		foreach (GameObject beaconInstance in missionController.beaconInstanceList)
		{
			beaconInstance.GetComponent<EntityStateMachine>().SetNextState(new NotReady());
		}
	}

	private void ServerFixedUpdate()
	{
		if (base.fixedAge >= transitionDuration)
		{
			missionController.ExitTransitionIntoBossfight();
			if (!hasSpawnedBoss)
			{
				SpawnBoss();
			}
			else if (scriptedCombatEncounter.combatSquad.readOnlyMembersList.Count == 0)
			{
				outer.SetNextState(new Exit());
				if (serverCycleCount < 1)
				{
					GoldshoresBossfight.onOneCycleGoldTitanKill?.Invoke();
				}
				return;
			}
		}
		if (!scriptedCombatEncounter)
		{
			return;
		}
		if (!bossImmunity)
		{
			if (bossInvulnerabilityStartTime.hasPassed)
			{
				ExtinguishBeacons();
				SetBossImmunity(newBossImmunity: true);
				serverCycleCount++;
			}
		}
		else if (missionController.beaconsActive >= missionController.beaconsToSpawnOnMap)
		{
			SetBossImmunity(newBossImmunity: false);
			bossInvulnerabilityStartTime = Run.FixedTimeStamp.now + shieldRemovalDuration;
		}
	}

	private void SpawnBoss()
	{
		if (!hasSpawnedBoss)
		{
			if (!scriptedCombatEncounter)
			{
				scriptedCombatEncounter = UnityEngine.Object.Instantiate(combatEncounterPrefab).GetComponent<ScriptedCombatEncounter>();
				scriptedCombatEncounter.GetComponent<BossGroup>().dropPosition = missionController.bossSpawnPosition;
				NetworkServer.Spawn(scriptedCombatEncounter.gameObject);
			}
			scriptedCombatEncounter.BeginEncounter();
			hasSpawnedBoss = scriptedCombatEncounter.hasSpawnedServer;
			if (hasSpawnedBoss)
			{
				SetBossImmunity(newBossImmunity: true);
			}
		}
	}
}
